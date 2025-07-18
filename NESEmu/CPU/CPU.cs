using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static NESEmu.NESEmulator;

namespace NESEmu.CPU {
   /// <summary>
   /// Instance of 6502 CPU Emulator
   /// </summary>
   public class CPU {
      // CPU Flags
      private bool CarryFlag;
      private bool ZeroFlag;
      //Apparently starts enabled
      private bool InteruptDisableFlag = true;
      /// <summary>
      /// This flag is ignored on the NES 6502 as it does not have a BCD Mode
      /// </summary>
      private bool DecimalModeFlag;
      private bool BreakFlag;
      private bool OverflowFlag;
      private bool NegativeFlag;

      //Important as NMIs can override IRQ even 4 ticks into IRQ
      private bool IsPendingNMI = false;
      private bool IsPendingIRQ = false;
      public Int64 CycleCount { get; private set; } = 0;
      private int IntFromBool(bool answer) {
         return (answer ? 1 : 0);
      }
      private bool BoolFromInt(int number) {
         return (number & 0x01) == 1;
      }
      /// <summary>
      /// Reads the processor flag state so that it can be pushed or pulled from the stack.
      /// </summary>
      private byte GetProcessorState() {
         int output = 0;
         output |= IntFromBool(CarryFlag);
         output |= (IntFromBool(ZeroFlag) << 1);
         output |= (IntFromBool(InteruptDisableFlag) << 2);
         output |= (IntFromBool(DecimalModeFlag) << 3);
         output |= (IntFromBool(BreakFlag) << 4);
         output |= (IntFromBool(OverflowFlag) << 5);
         output |= (IntFromBool(NegativeFlag) << 6);
         return (byte)output;
      }
      /// <summary>
      /// Can load the value from GetProcessorState() back into the CPU Main State
      /// </summary>
      private void LoadProcessorState(byte state) {
         CarryFlag = BoolFromInt(state);
         ZeroFlag = BoolFromInt(state >> 1);
         InteruptDisableFlag = BoolFromInt(state >> 2);
         DecimalModeFlag = BoolFromInt(state >> 3);
         BreakFlag = BoolFromInt(state >> 4);
         OverflowFlag = BoolFromInt(state >> 5);
         NegativeFlag = BoolFromInt(state >> 6);
      }

      // CPU Registers
      private ushort ProgramCounter;
      private byte StackPointer = 0xFD;
      private byte Accumulator;
      private byte XRegister;
      private byte YRegister;

      /// <summary>
      /// This is used to skip a cycle when the CPU instruction takes multiple cycles.
      /// </summary>
      private int DelegatedCycles;
      /// <summary>
      /// This can be an action to do after the CPU is done waiting.
      /// </summary>
      private Action? DelegatedAction;

      /// <summary>
      /// How the CPU should read memory.
      /// Should be overwritten when initialized.
      /// </summary>
      public BusReadDelegate ReadMemory = (address) => 0xEA;
      /// <summary>
      /// How the CPU should write to memory.
      /// Should be overwritten when initialized.
      /// </summary>
      public BusWriteDelegate WriteMemory = (address, data) => { return; };

      /// <summary>
      /// Used in the lookup table used with each instruction. 
      /// </summary>
      private struct OPCodeInfo {
         /// <summary>
         /// Function that pertains to each instruction.
         /// If this instruction reads a value such as ADC, this should expect the operand value.
         /// In other cases like STA, this function should expect a memory address.
         /// </summary>
         //It can either get value or memory address because functions that read a value need to
         //be able to accept an immediate value.
         public Action<ushort> ExecuteFunction;
         /// <summary>
         /// In a function that reads a value, such as ADC, this is expected to return that value in memory.
         /// In any other case, this should return the memory address.
         /// </summary>
         public Func<ushort> GetOperand;
         /// <summary>
         /// How many cycles this opcode usually takes to execute.
         /// Page Crossing Penalty should be handled by GetOperand, but is sometimes handled by ExecuteFunction.
         /// </summary>
         public int CycleTime;
      }

      private readonly OPCodeInfo[] InstructionTable = new OPCodeInfo[256];

      private byte PopProgramByte() {
         ProgramCounter++;
         return ReadMemory((ushort)(ProgramCounter - 1));
      }

      private byte PopStackByte() {
         StackPointer++;
         return ReadMemory((ushort)((StackPointer - 1) + 0x0100));
      }
      private void PushStackByte(byte Byte) {
         StackPointer--;
         WriteMemory((ushort)(0x0100 + StackPointer), Byte);
      }
      //LSB Numbers
      private ushort PopStackUShort() {
         //                 LSB                MSB
         return (ushort)(PopStackByte() | (PopStackByte() << 8));
      }
      private void PushStackUShort(ushort Ushort) {
         //MSB
         PushStackByte((byte)(Ushort >> 8));
         //LSB
         PushStackByte((byte)Ushort);
      }
      /// <summary>
      /// Will delay doing this action for an amount of cycles. When this action is finally performed, the cycle ends.
      /// This will override the previous Delegated Action.
      /// </summary>
      /// <param name="cycles">Cycles to wait</param>
      /// <param name="action">Action to be taken</param>
      public void DelegateCycles(int cycles, Action action) {
         DelegatedCycles += cycles;
         DelegatedAction = action;
      }
      /// <summary>
      /// The processor will do nothing for an amount of cycles.
      /// Does not actually wait any cycles before execute next line. Use DelegateCycles instead.
      /// </summary>
      /// <param name="cycles">Cycles to wait</param>
      public void WaitCycles(int cycles) {
         DelegatedCycles += cycles;
         DelegatedAction = null;
      }
      /// <summary>
      /// Clears any pending action from the cpu on the next cycles. 
      /// </summary>
      public void ClearWait() {
         DelegatedCycles = 0;
         DelegatedAction = null;
      }
      private bool IsCrossingPage(ushort newAddress, byte offset) {
         var dif = (ushort)(newAddress - offset);
         return (dif & 0xFF00) != (newAddress & 0xFF00);
      }
      //Get Operands from different CPU Addressing Modes
      /// <summary>
      /// The struct always expects a value, but on many instructions no argument is necessary
      /// </summary>
      private ushort ImpliedOperand() => 0;
      private ushort AccumulatorOperand() => Accumulator;
      private ushort PopImmediateOperand() => PopProgramByte();
      
      private ushort PopZeroPageLocation() => PopProgramByte();
      private ushort PopZeroPageOperand() => ReadMemory(PopProgramByte());

      private ushort PopZeroPageXLocation() => (byte)(PopProgramByte() + XRegister);
      private ushort PopZeroPageXOperand() => ReadMemory((byte)(PopProgramByte() + XRegister));

      private ushort PopZeroPageYLocation() => (byte)(PopProgramByte() + YRegister);
      private ushort PopZeroPageYOperand() => ReadMemory((byte)(PopProgramByte() + YRegister));

      /// <summary>
      /// Should be treated as an sbyte.
      /// </summary>
      private ushort PopRelativeOperand() => PopProgramByte();

      private ushort PopAbsoluteLocation() => (ushort)(PopProgramByte() | (PopProgramByte() << 8));
      private ushort PopAbsoluteOperand() => ReadMemory((ushort)(PopProgramByte() | (PopProgramByte() << 8)));
      private ushort PopAbsoluteXLocation() => (ushort)((PopProgramByte() | (PopProgramByte() << 8)) + XRegister);
      private ushort PopAbsoluteXOperandNoPC() => ReadMemory(PopAbsoluteXLocation());
      /// <summary>
      /// Read the Absolute,X operand and return the data at the location specified.
      /// Calls WaitCycles(1) If page crossed, so it can overwrite waiting action
      /// </summary>
      private ushort PopAbsoluteXOperandWPCPenalty() {
         var newLocation = PopAbsoluteXLocation();
         if (IsCrossingPage(newLocation, XRegister))
            WaitCycles(1);
         return newLocation;
      }
      private ushort PopAbsoluteYLocation() => (ushort)((PopProgramByte() | (PopProgramByte() << 8)) + YRegister);
      private ushort PopAbsoluteYOperandNoPC() => ReadMemory(PopAbsoluteYLocation());
      /// <summary>
      /// Read the Absolute,X operand and return the data at the location specified.
      /// Calls WaitCycles(1) If page crossed, so it can overwrite waiting action
      /// </summary>
      private ushort PopAbsoluteYOperandWPCPenalty() {
         var newLocation = PopAbsoluteYLocation();
         if (IsCrossingPage(newLocation, YRegister))
            WaitCycles(1);
         return newLocation;
      }
      /// <summary>
      /// Uses a full base address to indirectly specify the new location.
      /// Contains a bug where the page is incorrectly handled if located on a page boundary.
      /// </summary>
      private ushort PopIndirectLocationWJMPBug() {
         var location = PopAbsoluteLocation();
         var newLocation = (ushort)(ReadMemory(location) | ReadMemory((ushort)(location + 1)) << 8);
         if ((newLocation & 0x00FF) == 0x00FF) {
            newLocation -= 256;
         }
         return newLocation;
      }
      //Refered to as IndX by eunm of opcodes
      private ushort PopIndexedIndirectXLocation() {
         var location = (byte)(PopProgramByte() + XRegister);
         return (ushort)(ReadMemory(location) | ReadMemory((ushort)(location + 1)) << 8);
      }
      private ushort PopIndexedIndirectXOperand() => ReadMemory(PopIndexedIndirectXLocation());
      //Refered to as IndY by enum of opcodes
      private ushort PopIndirectIndexedYLocation() {
         var tableEntry = PopProgramByte();
         return (ushort)(ReadMemory(tableEntry) | (ReadMemory((ushort)(tableEntry + 1)) << 8));
      }
      private ushort PopIndirectIndexedYOperandWPGPenalty() {
         var newLocation = PopIndirectIndexedYLocation();
         if (IsCrossingPage((ushort)newLocation, YRegister))
            WaitCycles(1);
         return ReadMemory((ushort)(newLocation + YRegister));
      }
      public void ResetCPU() {
         DelegateCycles(6, () => {
            var startLocation = (ushort)(ReadMemory(0xFFFC) | (ReadMemory(0xFFFD) << 8));
            JMP(startLocation);
         });
      }
      public void TriggerInteruptIRQ() {
         if (InteruptDisableFlag)
            return;
         IsPendingIRQ = true;
      }
      public void TriggerInteruptNMI() {
         IsPendingNMI = true;
      }
      public void ExecuteCPUCyles(int cycles) {
         for (int i = 0; i < cycles; i++) {
            CycleCount++;

            if (DelegatedCycles > 0) {
               DelegatedCycles--;
               if (DelegatedCycles == 0 && DelegatedAction != null) {
                  DelegatedAction();
               }
               continue;
            }
            //Interupt Handler
            if (IsPendingIRQ || IsPendingNMI) {
               BreakFlag = true;
               PHP(0);
               PHA(0);
               PushStackUShort(ProgramCounter);
               DelegateCycles(3, () => {
                  ushort address;
                  if (IsPendingNMI)
                     address = (ushort)(ReadMemory(0xFFFA) | ReadMemory(0xFFFB) << 8);
                  else
                     address = (ushort)(ReadMemory(0xFFFE) | ReadMemory(0xFFFF) << 8);
                  IsPendingIRQ = false;
                  IsPendingNMI = false;
                  JMP(address);
                  WaitCycles(2);
               });
               continue;
            }

            byte instruction = PopProgramByte();
            var instructionInfo = InstructionTable[instruction];
            if (instructionInfo.CycleTime == 0) {
               throw new CPUException("Invalid Instruction Found.", instruction, (ushort)(ProgramCounter - 1));
            }
            var operand = instructionInfo.GetOperand();
            if (instructionInfo.CycleTime == 1) {
               instructionInfo.ExecuteFunction(operand);
               continue;
            }
            DelegateCycles(instructionInfo.CycleTime - 1, () => instructionInfo.ExecuteFunction(operand));
         }
      }
      // Instruction Functions
      private void ADC(ushort operand) {
         var sum = operand + Accumulator + (CarryFlag ? 1 : 0);
         OverflowFlag = (operand & 0x80) == (Accumulator & 0x80) && (operand & 0x80) != ((byte)sum & 0x80);
         Accumulator = (byte)sum;
         CarryFlag = sum > 255;
         ZeroFlag = Accumulator == 0;
         NegativeFlag = Accumulator > 127;
      }

      private void AND(ushort operand) {
         Accumulator = (byte)(operand & Accumulator);
         ZeroFlag = Accumulator == 0;
         NegativeFlag = Accumulator > 127;
      }
      private void ASL_Acc(ushort _) {
         CarryFlag = (Accumulator & 0x80) != 0;
         Accumulator = (byte)(Accumulator << 1);
         ZeroFlag = Accumulator == 0;
         NegativeFlag = Accumulator > 127;
      }
      private void ASL(ushort location) {
         var value = ReadMemory(location);
         CarryFlag = (value & 0x80) != 0;
         var newValue = (byte)(value << 1);
         ZeroFlag = newValue == 0;
         NegativeFlag = newValue > 127;
         WriteMemory(location, value);
      }
      private void BranchWithPenalty(Func<bool> condition, ushort relative) {
         var newValue = (ushort)((int)ProgramCounter + (sbyte)relative);
         WaitCycles(((newValue & 0xF0) == (ProgramCounter & 0xF0)) ? 1 : 3);
         ProgramCounter = newValue;
      }
      private void BIT(ushort location) {
         var result = ReadMemory(location);
         OverflowFlag = (result & 0b01000000) != 0;
         NegativeFlag = (result & 0x80) != 0;
         result &= Accumulator;
         ZeroFlag = result == 0;
      }
      private void CMP(ushort operand) {
         CarryFlag = Accumulator >= operand;
         ZeroFlag = Accumulator == operand;
         NegativeFlag = ((Accumulator + operand) & 0x80) != 0;
      }
      private void CPX(ushort operand) {
         CarryFlag = XRegister >= operand;
         ZeroFlag = XRegister == operand;
         NegativeFlag = ((XRegister + operand) & 0x80) != 0;
      }
      private void CPY(ushort operand) {
         CarryFlag = YRegister >= operand;
         ZeroFlag = YRegister == operand;
         NegativeFlag = ((YRegister + operand) & 0x80) != 0;
      }
      private void DEC(ushort location) {
         var value = ReadMemory(location);
         value--;
         ZeroFlag = value == 0;
         NegativeFlag = (value & 0x80) != 0;
         WriteMemory(location, value);
      }
      private void DEX(ushort _) {
         XRegister--;
         ZeroFlag = XRegister == 0;
         NegativeFlag = (XRegister & 0x80) != 0;
      }
      private void DEY(ushort _) {
         YRegister--;
         ZeroFlag = YRegister == 0;
         NegativeFlag = (YRegister & 0x80) != 0;
      }
      private void EOR(ushort operand) {
         Accumulator ^= (byte)operand;
         ZeroFlag = Accumulator == 0;
         NegativeFlag = Accumulator > 127;
      }
      private void INC(ushort location) {
         var value = ReadMemory(location);
         value++;
         ZeroFlag = value == 0;
         NegativeFlag = (value & 0x80) != 0;
         WriteMemory(location, value);
      }
      private void INX(ushort _) {
         XRegister++;
         ZeroFlag = XRegister == 0;
         NegativeFlag = (XRegister & 0x80) != 0;
      }
      private void INY(ushort _) {
         YRegister++;
         ZeroFlag = YRegister == 0;
         NegativeFlag = (YRegister & 0x80) != 0;
      }
      private void JMP(ushort location) {
         ProgramCounter = location;
      }
      private void JSR(ushort location) {
         PushStackUShort((ushort)(ProgramCounter - 1));
         JMP(location);
      }
      private void LDA(ushort operand) {
         Accumulator = (byte)operand;
         ZeroFlag = Accumulator == 0;
         NegativeFlag = Accumulator > 127;
      }
      private void LDX(ushort operand) {
         XRegister = (byte)operand;
         ZeroFlag = XRegister == 0;
         NegativeFlag = XRegister > 127;
      }
      private void LDY(ushort operand) {
         YRegister = (byte)operand;
         ZeroFlag = YRegister == 0;
         NegativeFlag = YRegister > 127;
      }
      private void LSR_Acc(ushort _) {
         CarryFlag = (Accumulator & 0x01) != 0;
         Accumulator = (byte)(Accumulator >> 1);
         ZeroFlag = Accumulator == 0;
         NegativeFlag = Accumulator > 127;
      }
      private void LSR(ushort location) {
         var value = ReadMemory(location);
         CarryFlag = (value & 0x01) != 0;
         var newValue = (byte)(value >> 1);
         ZeroFlag = newValue == 0;
         //Could never be negative after LSR
         NegativeFlag = false;
         WriteMemory(location, value);
      }
      private void ORA(ushort operand) {
         Accumulator |= (byte)operand;
         ZeroFlag = Accumulator == 0;
         NegativeFlag = Accumulator > 127;
      }
      private void PHA(ushort _) {
         PushStackByte(Accumulator);
      }
      private void PHP(ushort _) {
         PushStackByte(GetProcessorState());
      }
      private void PLA(ushort _) {
         Accumulator = PopStackByte();
         ZeroFlag = Accumulator == 0;
         NegativeFlag = Accumulator > 127;
      }
      private void PLP(ushort _) {
         LoadProcessorState(PopStackByte());
      }
      private void ROL_Acc(ushort _) {
         var oldCarryFlag = CarryFlag;
         CarryFlag = (Accumulator & 0x80) != 0;
         Accumulator = (byte)((Accumulator << 1) + (oldCarryFlag ? 1 : 0));
         NegativeFlag = Accumulator > 127;
      }
      private void ROL(ushort location) {
         var value = ReadMemory(location);
         var oldCarryFlag = CarryFlag;
         CarryFlag = (value & 0x80) != 0;
         value = (byte)((value << 1) + (oldCarryFlag ? 1 : 0));
         NegativeFlag = value > 127;
         WriteMemory(location, value);
      }
      private void ROR_Acc(ushort _) {
         var oldCarryFlag = CarryFlag;
         CarryFlag = (Accumulator & 0x01) != 0;
         Accumulator = (byte)((Accumulator >> 1) + (oldCarryFlag ? 128 : 0));
         NegativeFlag = Accumulator > 127;
      }
      private void ROR(ushort location) {
         var value = ReadMemory(location);
         var oldCarryFlag = CarryFlag;
         CarryFlag = (value & 0x01) != 0;
         value = (byte)((value >> 1) + (oldCarryFlag ? 128 : 0));
         NegativeFlag = value > 127;
         WriteMemory(location, value);
      }
      private void RTI(ushort _) {
         var previousAddress = PopStackUShort();
         PLA(0);
         PLP(0);
         JMP(previousAddress);
      }
      private void RTS(ushort _) {
         JMP(PopStackUShort());
      }
      private void SBC(ushort operand) {
         var diff = Accumulator - operand - (CarryFlag ? 0 : 1);
         OverflowFlag = ((Accumulator ^ operand) & 0x80) != 0 && ((Accumulator ^ diff) & 0x80) != 0;
         Accumulator = (byte)diff;
         CarryFlag = diff >= 0;
         ZeroFlag = Accumulator == 0;
         NegativeFlag = Accumulator > 127;
      }
      private void STA(ushort location) {
         WriteMemory(location, Accumulator);
      }
      private void STX(ushort location) {
         WriteMemory(location, XRegister);
      }
      private void STY(ushort location) {
         WriteMemory(location, YRegister);
      }
      private void Transfer(ref byte from, ref byte to) {
         to = from;
         ZeroFlag = from == 0;
         CarryFlag = from > 127;
      }

      // Init OpCodes
      public CPU() {
         //Initialize CPU
         WaitCycles(2);
         ResetCPU();
         //I realized about 1/3 of the way through the instructions that I probably could've abstracted this with functions to make it cleaner, but too bad ig
         //I already made snippets to quickly get through the instructions
         //This way is more "readable" and "efficient"
         InstructionTable[_6502OPCode.ADC_Imm] = new OPCodeInfo {
            ExecuteFunction = ADC,
            GetOperand = PopImmediateOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.ADC_Zer] = new OPCodeInfo {
            ExecuteFunction = ADC,
            GetOperand = PopZeroPageOperand,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.ADC_ZerX] = new OPCodeInfo {
            ExecuteFunction = ADC,
            GetOperand = PopZeroPageXOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.ADC_Abs] = new OPCodeInfo {
            ExecuteFunction = ADC,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.ADC_AbsX] = new OPCodeInfo {
            ExecuteFunction = ADC,
            GetOperand = PopAbsoluteXOperandWPCPenalty,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.ADC_AbsY] = new OPCodeInfo {
            ExecuteFunction = ADC,
            GetOperand = PopAbsoluteYOperandWPCPenalty,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.ADC_IndX] = new OPCodeInfo {
            ExecuteFunction = ADC,
            GetOperand = PopIndexedIndirectXOperand,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.ADC_IndY] = new OPCodeInfo {
            ExecuteFunction = ADC,
            GetOperand = PopIndirectIndexedYOperandWPGPenalty,
            CycleTime = 5
         };

         InstructionTable[_6502OPCode.AND_Imm] = new OPCodeInfo {
            ExecuteFunction = AND,
            GetOperand = PopImmediateOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.AND_Zer] = new OPCodeInfo {
            ExecuteFunction = AND,
            GetOperand = PopZeroPageOperand,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.AND_ZerX] = new OPCodeInfo {
            ExecuteFunction = AND,
            GetOperand = PopZeroPageXOperand,
            CycleTime = 4
         };
         InstructionTable[_6502OPCode.AND_Abs] = new OPCodeInfo {
            ExecuteFunction = AND,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 4
         };
         InstructionTable[_6502OPCode.AND_AbsX] = new OPCodeInfo {
            ExecuteFunction = AND,
            GetOperand = PopAbsoluteXOperandWPCPenalty,
            CycleTime = 4
         };
         InstructionTable[_6502OPCode.AND_AbsY] = new OPCodeInfo {
            ExecuteFunction = AND,
            GetOperand = PopAbsoluteYOperandWPCPenalty,
            CycleTime = 4
         };
         InstructionTable[_6502OPCode.AND_IndX] = new OPCodeInfo {
            ExecuteFunction = AND,
            GetOperand = PopIndexedIndirectXLocation,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.AND_IndY] = new OPCodeInfo {
            ExecuteFunction = AND,
            GetOperand = PopIndirectIndexedYLocation,
            CycleTime = 5
         };

         InstructionTable[_6502OPCode.ASL_Acc] = new OPCodeInfo {
            ExecuteFunction = ASL_Acc,
            GetOperand = AccumulatorOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.ASL_Zer] = new OPCodeInfo {
            ExecuteFunction = ASL,
            GetOperand = PopZeroPageLocation,
            CycleTime = 5
         };
         InstructionTable[_6502OPCode.ASL_ZerX] = new OPCodeInfo {
            ExecuteFunction = ASL,
            GetOperand = PopZeroPageXLocation,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.ASL_Abs] = new OPCodeInfo {
            ExecuteFunction = ASL,
            GetOperand = PopAbsoluteLocation,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.ASL_AbsX] = new OPCodeInfo {
            ExecuteFunction = ASL,
            GetOperand = PopAbsoluteXLocation,
            CycleTime = 7
         };

         InstructionTable[_6502OPCode.BCC_Rel] = new OPCodeInfo {
            ExecuteFunction = (operand) => BranchWithPenalty(() => !CarryFlag, operand),
            GetOperand = PopRelativeOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.BCS_Rel] = new OPCodeInfo {
            ExecuteFunction = (operand) => BranchWithPenalty(() => CarryFlag, operand),
            GetOperand = PopRelativeOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.BIT_Zer] = new OPCodeInfo {
            ExecuteFunction = BIT,
            GetOperand = PopZeroPageOperand,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.BIT_Abs] = new OPCodeInfo {
            ExecuteFunction = BIT,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 4
         };

         InstructionTable[_6502OPCode.BMI_Rel] = new OPCodeInfo {
            ExecuteFunction = (operand) => BranchWithPenalty(() => NegativeFlag, operand),
            GetOperand = PopRelativeOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.BNE_Rel] = new OPCodeInfo {
            ExecuteFunction = (operand) => BranchWithPenalty(() => !ZeroFlag, operand),
            GetOperand = PopRelativeOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.BPL_Rel] = new OPCodeInfo {
            ExecuteFunction = (operand) => BranchWithPenalty(() => !NegativeFlag, operand),
            GetOperand = PopRelativeOperand,
            CycleTime = 2
         };

         //The TriggerInterupt Function should handle how long it takes to cycle
         InstructionTable[_6502OPCode.BRK] = new OPCodeInfo {
            ExecuteFunction = (operand) => TriggerInteruptIRQ(),
            GetOperand = ImpliedOperand,
            CycleTime = 1
         };

         InstructionTable[_6502OPCode.BVC_Rel] = new OPCodeInfo {
            ExecuteFunction = (operand) => BranchWithPenalty(() => !OverflowFlag, operand),
            GetOperand = PopRelativeOperand,
            CycleTime = 2
         };


         InstructionTable[_6502OPCode.BVS_Rel] = new OPCodeInfo {
            ExecuteFunction = (operand) => BranchWithPenalty(() => !OverflowFlag, operand),
            GetOperand = PopRelativeOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.CLC] = new OPCodeInfo {
            ExecuteFunction = (operand) => CarryFlag = false,
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.CLD] = new OPCodeInfo {
            ExecuteFunction = (operand) => DecimalModeFlag = false,
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.CLI] = new OPCodeInfo {
            ExecuteFunction = (operand) => InteruptDisableFlag = false,
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.CLV] = new OPCodeInfo {
            ExecuteFunction = (operand) => OverflowFlag = false,
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.CMP_Imm] = new OPCodeInfo {
            ExecuteFunction = CMP,
            GetOperand = PopImmediateOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.CMP_Zer] = new OPCodeInfo {
            ExecuteFunction = CMP,
            GetOperand = PopZeroPageOperand,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.CMP_ZerX] = new OPCodeInfo {
            ExecuteFunction = CMP,
            GetOperand = PopZeroPageXOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.CMP_Abs] = new OPCodeInfo {
            ExecuteFunction = CMP,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.CMP_AbsX] = new OPCodeInfo {
            ExecuteFunction = CMP,
            GetOperand = PopAbsoluteXOperandWPCPenalty,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.CMP_AbsY] = new OPCodeInfo {
            ExecuteFunction = CMP,
            GetOperand = PopAbsoluteYOperandWPCPenalty,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.CMP_IndX] = new OPCodeInfo {
            ExecuteFunction = CMP,
            GetOperand = PopIndexedIndirectXOperand,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.CMP_IndY] = new OPCodeInfo {
            ExecuteFunction = CMP,
            GetOperand = PopIndirectIndexedYOperandWPGPenalty,
            CycleTime = 5
         };

         InstructionTable[_6502OPCode.CPX_Imm] = new OPCodeInfo {
            ExecuteFunction = CPX,
            GetOperand = PopImmediateOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.CPX_Zer] = new OPCodeInfo {
            ExecuteFunction = CPX,
            GetOperand = PopZeroPageOperand,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.CPX_Abs] = new OPCodeInfo {
            ExecuteFunction = CPX,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 4
         };

         InstructionTable[_6502OPCode.CPY_Imm] = new OPCodeInfo {
            ExecuteFunction = CPY,
            GetOperand = PopImmediateOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.CPY_Zer] = new OPCodeInfo {
            ExecuteFunction = CPY,
            GetOperand = PopZeroPageOperand,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.CPY_Abs] = new OPCodeInfo {
            ExecuteFunction = CPY,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 4
         };

         InstructionTable[_6502OPCode.DEC_Zer] = new OPCodeInfo {
            ExecuteFunction = DEC,
            GetOperand = PopZeroPageOperand,
            CycleTime = 5
         };
         InstructionTable[_6502OPCode.DEC_ZerX] = new OPCodeInfo {
            ExecuteFunction = DEC,
            GetOperand = PopZeroPageXOperand,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.DEC_Abs] = new OPCodeInfo {
            ExecuteFunction = DEC,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.DEC_AbsX] = new OPCodeInfo {
            ExecuteFunction = DEC,
            GetOperand = PopAbsoluteXOperandNoPC,
            CycleTime = 7
         };

         InstructionTable[_6502OPCode.DEX] = new OPCodeInfo {
            ExecuteFunction = DEX,
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.DEY] = new OPCodeInfo {
            ExecuteFunction = DEY,
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.EOR_Imm] = new OPCodeInfo {
            ExecuteFunction = EOR,
            GetOperand = PopImmediateOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.EOR_Zer] = new OPCodeInfo {
            ExecuteFunction = EOR,
            GetOperand = PopZeroPageOperand,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.EOR_ZerX] = new OPCodeInfo {
            ExecuteFunction = EOR,
            GetOperand = PopZeroPageXOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.EOR_Abs] = new OPCodeInfo {
            ExecuteFunction = EOR,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.EOR_AbsX] = new OPCodeInfo {
            ExecuteFunction = EOR,
            GetOperand = PopAbsoluteXOperandWPCPenalty,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.EOR_AbsY] = new OPCodeInfo {
            ExecuteFunction = EOR,
            GetOperand = PopAbsoluteYOperandWPCPenalty,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.EOR_IndX] = new OPCodeInfo {
            ExecuteFunction = EOR,
            GetOperand = PopIndexedIndirectXOperand,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.EOR_IndY] = new OPCodeInfo {
            ExecuteFunction = EOR,
            GetOperand = PopIndirectIndexedYOperandWPGPenalty,
            CycleTime = 5
         };

         InstructionTable[_6502OPCode.INC_Zer] = new OPCodeInfo {
            ExecuteFunction = INC,
            GetOperand = PopZeroPageOperand,
            CycleTime = 5
         };
         InstructionTable[_6502OPCode.INC_ZerX] = new OPCodeInfo {
            ExecuteFunction = INC,
            GetOperand = PopZeroPageXOperand,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.INC_Abs] = new OPCodeInfo {
            ExecuteFunction = INC,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.INC_AbsX] = new OPCodeInfo {
            ExecuteFunction = INC,
            GetOperand = PopAbsoluteXOperandNoPC,
            CycleTime = 7
         };

         InstructionTable[_6502OPCode.INX] = new OPCodeInfo {
            ExecuteFunction = INX,
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.INY] = new OPCodeInfo {
            ExecuteFunction = INY,
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.JMP_Abs] = new OPCodeInfo {
            ExecuteFunction = JMP,
            GetOperand = PopAbsoluteLocation,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.JMP_Ind] = new OPCodeInfo {
            ExecuteFunction = JMP,
            GetOperand = PopIndirectLocationWJMPBug,
            CycleTime = 5
         };

         InstructionTable[_6502OPCode.JSR_Abs] = new OPCodeInfo {
            ExecuteFunction = JSR,
            GetOperand = PopAbsoluteLocation,
            CycleTime = 6
         };

         InstructionTable[_6502OPCode.LDA_Imm] = new OPCodeInfo {
            ExecuteFunction = LDA,
            GetOperand = PopImmediateOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.LDA_Zer] = new OPCodeInfo {
            ExecuteFunction = LDA,
            GetOperand = PopZeroPageOperand,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.LDA_ZerX] = new OPCodeInfo {
            ExecuteFunction = LDA,
            GetOperand = PopZeroPageXOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.LDA_Abs] = new OPCodeInfo {
            ExecuteFunction = LDA,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.LDA_AbsX] = new OPCodeInfo {
            ExecuteFunction = LDA,
            GetOperand = PopAbsoluteXOperandWPCPenalty,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.LDA_AbsY] = new OPCodeInfo {
            ExecuteFunction = LDA,
            GetOperand = PopAbsoluteYOperandWPCPenalty,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.LDA_IndX] = new OPCodeInfo {
            ExecuteFunction = LDA,
            GetOperand = PopIndexedIndirectXOperand,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.LDA_IndY] = new OPCodeInfo {
            ExecuteFunction = LDA,
            GetOperand = PopIndirectIndexedYOperandWPGPenalty,
            CycleTime = 5
         };

         InstructionTable[_6502OPCode.LDX_Imm] = new OPCodeInfo {
            ExecuteFunction = LDX,
            GetOperand = PopImmediateOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.LDX_Zer] = new OPCodeInfo {
            ExecuteFunction = LDX,
            GetOperand = PopZeroPageOperand,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.LDX_ZerY] = new OPCodeInfo {
            ExecuteFunction = LDX,
            GetOperand = PopZeroPageYOperand,
            CycleTime = 1
         };
         InstructionTable[_6502OPCode.LDX_Abs] = new OPCodeInfo {
            ExecuteFunction = LDX,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.LDX_AbsY] = new OPCodeInfo {
            ExecuteFunction = LDX,
            GetOperand = PopAbsoluteYOperandWPCPenalty,
            CycleTime = 4,
         };

         InstructionTable[_6502OPCode.LDY_Imm] = new OPCodeInfo {
            ExecuteFunction = LDY,
            GetOperand = PopImmediateOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.LDY_Zer] = new OPCodeInfo {
            ExecuteFunction = LDY,
            GetOperand = PopZeroPageOperand,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.LDY_ZerX] = new OPCodeInfo {
            ExecuteFunction = LDY,
            GetOperand = PopZeroPageXOperand,
            CycleTime = 1
         };
         InstructionTable[_6502OPCode.LDY_Abs] = new OPCodeInfo {
            ExecuteFunction = LDY,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.LDY_AbsX] = new OPCodeInfo {
            ExecuteFunction = LDY,
            GetOperand = PopAbsoluteXOperandWPCPenalty,
            CycleTime = 4,
         };

         InstructionTable[_6502OPCode.LSR_Acc] = new OPCodeInfo {
            ExecuteFunction = LSR_Acc,
            GetOperand = AccumulatorOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.LSR_Zer] = new OPCodeInfo {
            ExecuteFunction = LSR,
            GetOperand = PopZeroPageLocation,
            CycleTime = 5
         };
         InstructionTable[_6502OPCode.LSR_ZerX] = new OPCodeInfo {
            ExecuteFunction = LSR,
            GetOperand = PopZeroPageXLocation,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.LSR_Abs] = new OPCodeInfo {
            ExecuteFunction = LSR,
            GetOperand = PopAbsoluteLocation,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.LSR_AbsX] = new OPCodeInfo {
            ExecuteFunction = LSR,
            GetOperand = PopAbsoluteXLocation,
            CycleTime = 7
         };

         InstructionTable[_6502OPCode.NOP] = new OPCodeInfo {
            ExecuteFunction = (_) => { },
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.ORA_Imm] = new OPCodeInfo {
            ExecuteFunction = ORA,
            GetOperand = PopImmediateOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.ORA_Zer] = new OPCodeInfo {
            ExecuteFunction = ORA,
            GetOperand = PopZeroPageOperand,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.ORA_ZerX] = new OPCodeInfo {
            ExecuteFunction = ORA,
            GetOperand = PopZeroPageXOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.ORA_Abs] = new OPCodeInfo {
            ExecuteFunction = ORA,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.ORA_AbsX] = new OPCodeInfo {
            ExecuteFunction = ORA,
            GetOperand = PopAbsoluteXOperandWPCPenalty,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.ORA_AbsY] = new OPCodeInfo {
            ExecuteFunction = ORA,
            GetOperand = PopAbsoluteYOperandWPCPenalty,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.ORA_IndX] = new OPCodeInfo {
            ExecuteFunction = ORA,
            GetOperand = PopIndexedIndirectXOperand,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.ORA_IndY] = new OPCodeInfo {
            ExecuteFunction = ORA,
            GetOperand = PopIndirectIndexedYOperandWPGPenalty,
            CycleTime = 5
         };

         InstructionTable[_6502OPCode.PHA] = new OPCodeInfo {
            ExecuteFunction = PHA,
            GetOperand = ImpliedOperand,
            CycleTime = 3
         };

         InstructionTable[_6502OPCode.PHP] = new OPCodeInfo {
            ExecuteFunction = PHP,
            GetOperand = ImpliedOperand,
            CycleTime = 3
         };

         InstructionTable[_6502OPCode.PLA] = new OPCodeInfo {
            ExecuteFunction = PLA,
            GetOperand = ImpliedOperand,
            CycleTime = 4
         };

         InstructionTable[_6502OPCode.PLP] = new OPCodeInfo {
            ExecuteFunction = PLP,
            GetOperand = ImpliedOperand,
            CycleTime = 4
         };

         InstructionTable[_6502OPCode.ROL_Acc] = new OPCodeInfo {
            ExecuteFunction = ROL_Acc,
            GetOperand = AccumulatorOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.ROL_Zer] = new OPCodeInfo {
            ExecuteFunction = ROL,
            GetOperand = PopZeroPageLocation,
            CycleTime = 5
         };
         InstructionTable[_6502OPCode.ROL_ZerX] = new OPCodeInfo {
            ExecuteFunction = ROL,
            GetOperand = PopZeroPageXLocation,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.ROL_Abs] = new OPCodeInfo {
            ExecuteFunction = ROL,
            GetOperand = PopAbsoluteLocation,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.ROL_AbsX] = new OPCodeInfo {
            ExecuteFunction = ROL,
            GetOperand = PopAbsoluteXLocation,
            CycleTime = 7
         };

         InstructionTable[_6502OPCode.ROR_Acc] = new OPCodeInfo {
            ExecuteFunction = ROR_Acc,
            GetOperand = AccumulatorOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.ROR_Zer] = new OPCodeInfo {
            ExecuteFunction = ROR,
            GetOperand = PopZeroPageLocation,
            CycleTime = 5
         };
         InstructionTable[_6502OPCode.ROR_ZerX] = new OPCodeInfo {
            ExecuteFunction = ROR,
            GetOperand = PopZeroPageXLocation,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.ROR_Abs] = new OPCodeInfo {
            ExecuteFunction = ROR,
            GetOperand = PopAbsoluteLocation,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.ROR_AbsX] = new OPCodeInfo {
            ExecuteFunction = ROR,
            GetOperand = PopAbsoluteXLocation,
            CycleTime = 7
         };

         InstructionTable[_6502OPCode.RTI] = new OPCodeInfo {
            ExecuteFunction = RTI,
            GetOperand = ImpliedOperand,
            CycleTime = 6
         };

         InstructionTable[_6502OPCode.RTS] = new OPCodeInfo {
            ExecuteFunction = RTS,
            GetOperand = ImpliedOperand,
            CycleTime = 6
         };

         InstructionTable[_6502OPCode.SBC_Imm] = new OPCodeInfo {
            ExecuteFunction = SBC,
            GetOperand = PopImmediateOperand,
            CycleTime = 2
         };
         InstructionTable[_6502OPCode.SBC_Zer] = new OPCodeInfo {
            ExecuteFunction = SBC,
            GetOperand = PopZeroPageOperand,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.SBC_ZerX] = new OPCodeInfo {
            ExecuteFunction = SBC,
            GetOperand = PopZeroPageXOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.SBC_Abs] = new OPCodeInfo {
            ExecuteFunction = SBC,
            GetOperand = PopAbsoluteOperand,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.SBC_AbsX] = new OPCodeInfo {
            ExecuteFunction = SBC,
            GetOperand = PopAbsoluteXOperandWPCPenalty,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.SBC_AbsY] = new OPCodeInfo {
            ExecuteFunction = SBC,
            GetOperand = PopAbsoluteYOperandWPCPenalty,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.SBC_IndX] = new OPCodeInfo {
            ExecuteFunction = SBC,
            GetOperand = PopIndexedIndirectXOperand,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.SBC_IndY] = new OPCodeInfo {
            ExecuteFunction = SBC,
            GetOperand = PopIndirectIndexedYOperandWPGPenalty,
            CycleTime = 5
         };

         InstructionTable[_6502OPCode.SEC] = new OPCodeInfo {
            ExecuteFunction = (_) => CarryFlag = true,
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.SED] = new OPCodeInfo {
            ExecuteFunction = (_) => DecimalModeFlag = true,
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.SEI] = new OPCodeInfo {
            ExecuteFunction = (_) => InteruptDisableFlag = true,
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.STA_Zer] = new OPCodeInfo {
            ExecuteFunction = STA,
            GetOperand = PopZeroPageLocation,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.STA_ZerX] = new OPCodeInfo {
            ExecuteFunction = STA,
            GetOperand = PopZeroPageXLocation,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.STA_Abs] = new OPCodeInfo {
            ExecuteFunction = STA,
            GetOperand = PopAbsoluteLocation,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.STA_AbsX] = new OPCodeInfo {
            ExecuteFunction = STA,
            GetOperand = PopAbsoluteXLocation,
            CycleTime = 5,
         };
         InstructionTable[_6502OPCode.STA_AbsY] = new OPCodeInfo {
            ExecuteFunction = STA,
            GetOperand = PopAbsoluteYLocation,
            CycleTime = 5,
         };
         InstructionTable[_6502OPCode.STA_IndX] = new OPCodeInfo {
            ExecuteFunction = STA,
            GetOperand = PopIndexedIndirectXLocation,
            CycleTime = 6
         };
         InstructionTable[_6502OPCode.STA_IndY] = new OPCodeInfo {
            ExecuteFunction = STA,
            GetOperand = PopIndirectIndexedYLocation,
            CycleTime = 6
         };

         InstructionTable[_6502OPCode.STX_Zer] = new OPCodeInfo {
            ExecuteFunction = STX,
            GetOperand = PopZeroPageLocation,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.STX_ZerY] = new OPCodeInfo {
            ExecuteFunction = STX,
            GetOperand = PopZeroPageYLocation,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.STX_Abs] = new OPCodeInfo {
            ExecuteFunction = STX,
            GetOperand = PopAbsoluteLocation,
            CycleTime = 4,
         };

         InstructionTable[_6502OPCode.STY_Zer] = new OPCodeInfo {
            ExecuteFunction = STY,
            GetOperand = PopZeroPageLocation,
            CycleTime = 3
         };
         InstructionTable[_6502OPCode.STY_ZerX] = new OPCodeInfo {
            ExecuteFunction = STY,
            GetOperand = PopZeroPageXLocation,
            CycleTime = 4,
         };
         InstructionTable[_6502OPCode.STY_Abs] = new OPCodeInfo {
            ExecuteFunction = STY,
            GetOperand = PopAbsoluteLocation,
            CycleTime = 4,
         };

         InstructionTable[_6502OPCode.TAX] = new OPCodeInfo {
            ExecuteFunction = (_) => Transfer(ref Accumulator, ref XRegister),
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.TAY] = new OPCodeInfo {
            ExecuteFunction = (_) => Transfer(ref Accumulator, ref YRegister),
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.TSX] = new OPCodeInfo {
            ExecuteFunction = (_) => Transfer(ref StackPointer, ref XRegister),
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.TXA] = new OPCodeInfo {
            ExecuteFunction = (_) => Transfer(ref XRegister, ref Accumulator),
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.TXS] = new OPCodeInfo {
            ExecuteFunction = (_) => Transfer(ref XRegister, ref StackPointer),
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };

         InstructionTable[_6502OPCode.TYA] = new OPCodeInfo {
            ExecuteFunction = (_) => Transfer(ref YRegister, ref Accumulator),
            GetOperand = ImpliedOperand,
            CycleTime = 2
         };
      }
   }
}

using System.Linq;
using System.Reflection;

//Generated given a basic naming convention by claude.ai. No I was not going to do this by hand.

namespace NESEmu.CPU;

/// <summary>
/// Complete 6502 processor opcodes with all addressing modes.
/// The naming conventions are as follows: 
/// [Instruction]_[3 Digits of Addressing mode][X or Y if Used]
/// (The Addressing mode can be left off if implied addressing)
/// 
/// Addressing Mode Abbreviations:
/// Imm = Immediate (#$44)
/// Zer = Zero Page ($44)
/// ZerX/ZerY = Zero Page,X/Y ($44,X)
/// Abs = Absolute ($4400)
/// AbsX/AbsY = Absolute,X/Y ($4400,X)
/// IndX = Indexed Indirect (($44,X))
/// IndY = Indirect Indexed (($44),Y)
/// Ind = Indirect (($4400))
/// Rel = Relative (branch instructions)
/// Acc = Accumulator (implied)
/// 
/// Examples: 
/// ADC_ZerX, ADC_Abs, BMI_Rel, INX, NOP
/// </summary>
public static class _6502OPCode {
   /// <summary>
   /// Takes in a byte value and returns an opcode. This is very slow and should only be used in debugging.
   /// </summary>
   public static string GetOpCode(byte value) {
      var thisType = typeof(_6502OPCode);
      var fields = thisType.GetFields(BindingFlags.Public | BindingFlags.Static);
      return fields.FirstOrDefault((v) => v.IsLiteral && (byte)v.GetRawConstantValue() == value)?.Name ?? "Unknown";
   }
   // ADC - Add with Carry
   public const byte ADC_Imm = 0x69;
   public const byte ADC_Zer = 0x65;
   public const byte ADC_ZerX = 0x75;
   public const byte ADC_Abs = 0x6D;
   public const byte ADC_AbsX = 0x7D;
   public const byte ADC_AbsY = 0x79;
   public const byte ADC_IndX = 0x61;
   public const byte ADC_IndY = 0x71;

   // AND - Logical AND
   public const byte AND_Imm = 0x29;
   public const byte AND_Zer = 0x25;
   public const byte AND_ZerX = 0x35;
   public const byte AND_Abs = 0x2D;
   public const byte AND_AbsX = 0x3D;
   public const byte AND_AbsY = 0x39;
   public const byte AND_IndX = 0x21;
   public const byte AND_IndY = 0x31;

   // ASL - Arithmetic Shift Left
   public const byte ASL_Acc = 0x0A;
   public const byte ASL_Zer = 0x06;
   public const byte ASL_ZerX = 0x16;
   public const byte ASL_Abs = 0x0E;
   public const byte ASL_AbsX = 0x1E;

   // BCC - Branch if Carry Clear
   public const byte BCC_Rel = 0x90;

   // BCS - Branch if Carry Set
   public const byte BCS_Rel = 0xB0;

   // BEQ - Branch if Equal (Zero Set)
   public const byte BEQ_Rel = 0xF0;

   // BIT - Bit Test
   public const byte BIT_Zer = 0x24;
   public const byte BIT_Abs = 0x2C;

   // BMI - Branch if Minus (Negative Set)
   public const byte BMI_Rel = 0x30;

   // BNE - Branch if Not Equal (Zero Clear)
   public const byte BNE_Rel = 0xD0;

   // BPL - Branch if Plus (Negative Clear)
   public const byte BPL_Rel = 0x10;

   // BRK - Force Interrupt
   public const byte BRK = 0x00;

   // BVC - Branch if Overflow Clear
   public const byte BVC_Rel = 0x50;

   // BVS - Branch if Overflow Set
   public const byte BVS_Rel = 0x70;

   // CLC - Clear Carry Flag
   public const byte CLC = 0x18;

   // CLD - Clear Decimal Mode
   public const byte CLD = 0xD8;

   // CLI - Clear Interrupt Disable
   public const byte CLI = 0x58;

   // CLV - Clear Overflow Flag
   public const byte CLV = 0xB8;

   // CMP - Compare Accumulator
   public const byte CMP_Imm = 0xC9;
   public const byte CMP_Zer = 0xC5;
   public const byte CMP_ZerX = 0xD5;
   public const byte CMP_Abs = 0xCD;
   public const byte CMP_AbsX = 0xDD;
   public const byte CMP_AbsY = 0xD9;
   public const byte CMP_IndX = 0xC1;
   public const byte CMP_IndY = 0xD1;

   // CPX - Compare X Register
   public const byte CPX_Imm = 0xE0;
   public const byte CPX_Zer = 0xE4;
   public const byte CPX_Abs = 0xEC;

   // CPY - Compare Y Register
   public const byte CPY_Imm = 0xC0;
   public const byte CPY_Zer = 0xC4;
   public const byte CPY_Abs = 0xCC;

   // DEC - Decrement Memory
   public const byte DEC_Zer = 0xC6;
   public const byte DEC_ZerX = 0xD6;
   public const byte DEC_Abs = 0xCE;
   public const byte DEC_AbsX = 0xDE;

   // DEX - Decrement X Register
   public const byte DEX = 0xCA;

   // DEY - Decrement Y Register
   public const byte DEY = 0x88;

   // EOR - Exclusive OR
   public const byte EOR_Imm = 0x49;
   public const byte EOR_Zer = 0x45;
   public const byte EOR_ZerX = 0x55;
   public const byte EOR_Abs = 0x4D;
   public const byte EOR_AbsX = 0x5D;
   public const byte EOR_AbsY = 0x59;
   public const byte EOR_IndX = 0x41;
   public const byte EOR_IndY = 0x51;

   // INC - Increment Memory
   public const byte INC_Zer = 0xE6;
   public const byte INC_ZerX = 0xF6;
   public const byte INC_Abs = 0xEE;
   public const byte INC_AbsX = 0xFE;

   // INX - Increment X Register
   public const byte INX = 0xE8;

   // INY - Increment Y Register
   public const byte INY = 0xC8;

   // JMP - Jump
   public const byte JMP_Abs = 0x4C;
   public const byte JMP_Ind = 0x6C;

   // JSR - Jump to Subroutine
   public const byte JSR_Abs = 0x20;

   // LDA - Load Accumulator
   public const byte LDA_Imm = 0xA9;
   public const byte LDA_Zer = 0xA5;
   public const byte LDA_ZerX = 0xB5;
   public const byte LDA_Abs = 0xAD;
   public const byte LDA_AbsX = 0xBD;
   public const byte LDA_AbsY = 0xB9;
   public const byte LDA_IndX = 0xA1;
   public const byte LDA_IndY = 0xB1;

   // LDX - Load X Register
   public const byte LDX_Imm = 0xA2;
   public const byte LDX_Zer = 0xA6;
   public const byte LDX_ZerY = 0xB6;
   public const byte LDX_Abs = 0xAE;
   public const byte LDX_AbsY = 0xBE;

   // LDY - Load Y Register
   public const byte LDY_Imm = 0xA0;
   public const byte LDY_Zer = 0xA4;
   public const byte LDY_ZerX = 0xB4;
   public const byte LDY_Abs = 0xAC;
   public const byte LDY_AbsX = 0xBC;

   // LSR - Logical Shift Right
   public const byte LSR_Acc = 0x4A;
   public const byte LSR_Zer = 0x46;
   public const byte LSR_ZerX = 0x56;
   public const byte LSR_Abs = 0x4E;
   public const byte LSR_AbsX = 0x5E;

   // NOP - No Operation
   public const byte NOP = 0xEA;

   // ORA - Logical Inclusive OR
   public const byte ORA_Imm = 0x09;
   public const byte ORA_Zer = 0x05;
   public const byte ORA_ZerX = 0x15;
   public const byte ORA_Abs = 0x0D;
   public const byte ORA_AbsX = 0x1D;
   public const byte ORA_AbsY = 0x19;
   public const byte ORA_IndX = 0x01;
   public const byte ORA_IndY = 0x11;

   // PHA - Push Accumulator
   public const byte PHA = 0x48;

   // PHP - Push Processor Status
   public const byte PHP = 0x08;

   // PLA - Pull Accumulator
   public const byte PLA = 0x68;

   // PLP - Pull Processor Status
   public const byte PLP = 0x28;

   // ROL - Rotate Left
   public const byte ROL_Acc = 0x2A;
   public const byte ROL_Zer = 0x26;
   public const byte ROL_ZerX = 0x36;
   public const byte ROL_Abs = 0x2E;
   public const byte ROL_AbsX = 0x3E;

   // ROR - Rotate Right
   public const byte ROR_Acc = 0x6A;
   public const byte ROR_Zer = 0x66;
   public const byte ROR_ZerX = 0x76;
   public const byte ROR_Abs = 0x6E;
   public const byte ROR_AbsX = 0x7E;

   // RTI - Return from Interrupt
   public const byte RTI = 0x40;

   // RTS - Return from Subroutine
   public const byte RTS = 0x60;

   // SBC - Subtract with Carry
   public const byte SBC_Imm = 0xE9;
   public const byte SBC_Zer = 0xE5;
   public const byte SBC_ZerX = 0xF5;
   public const byte SBC_Abs = 0xED;
   public const byte SBC_AbsX = 0xFD;
   public const byte SBC_AbsY = 0xF9;
   public const byte SBC_IndX = 0xE1;
   public const byte SBC_IndY = 0xF1;

   // SEC - Set Carry Flag
   public const byte SEC = 0x38;

   // SED - Set Decimal Mode
   /// <summary>
   /// Pretty much useless on retail console, since BCD mode is ignored.
   /// </summary>
   public const byte SED = 0xF8;

   // SEI - Set Interrupt Disable
   public const byte SEI = 0x78;

   // STA - Store Accumulator
   public const byte STA_Zer = 0x85;
   public const byte STA_ZerX = 0x95;
   public const byte STA_Abs = 0x8D;
   public const byte STA_AbsX = 0x9D;
   public const byte STA_AbsY = 0x99;
   public const byte STA_IndX = 0x81;
   public const byte STA_IndY = 0x91;

   // STX - Store X Register
   public const byte STX_Zer = 0x86;
   public const byte STX_ZerY = 0x96;
   public const byte STX_Abs = 0x8E;

   // STY - Store Y Register
   public const byte STY_Zer = 0x84;
   public const byte STY_ZerX = 0x94;
   public const byte STY_Abs = 0x8C;

   // TAX - Transfer Accumulator to X
   public const byte TAX = 0xAA;

   // TAY - Transfer Accumulator to Y
   public const byte TAY = 0xA8;

   // TSX - Transfer Stack Pointer to X
   public const byte TSX = 0xBA;

   // TXA - Transfer X to Accumulator
   public const byte TXA = 0x8A;

   // TXS - Transfer X to Stack Pointer
   public const byte TXS = 0x9A;

   // TYA - Transfer Y to Accumulator
   public const byte TYA = 0x98;
}
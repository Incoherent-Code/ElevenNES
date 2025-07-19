using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NESEmu.CPU;
using static NESEmu.CPU._6502OPCode;

namespace NESEmuTests.CPU {
   [TestClass]
   public sealed class CPUTests {
      /// <summary>
      /// This test is based on the video by Ben Eater showcasing the 6502 clock init cycles.
      /// https://youtu.be/LnzuMJLZRdU?si=uq2lvz8GXYMqCg_0
      /// Relevant for this test is 21:44
      /// </summary>
      [TestMethod]
      public void BasicNopCycleAccurateTest() {
         var cpu = new NESEmu.CPU.CPU();
         ushort lastReadAddress = 0;
         cpu.ReadMemory = (ushort address) => {
            lastReadAddress = address;
            return 0xEA;
         };
         //Note: CPU starts execution after 9 cycles, so during the 10th cycle is when it should read the value
         //Odly enough, the 6502 in the video executes in only one cycle. Not sure but we will assume the cpu only takes 8 cycles to execute for now
         cpu.ExecuteCPUCyles(9);
         Assert.AreEqual(0xEAEA, lastReadAddress, "CPU should initialize and start execution at 0xEAEA.");
         cpu.ExecuteCPUCyles(2);
         Assert.AreEqual(0xEAEB, lastReadAddress, "CPU should just keep incrimenting with nop command every 2 cycles.");
         cpu.ExecuteCPUCyles(1);
         Assert.AreEqual(0xEAEB, lastReadAddress, "CPU shouldn't read again during the next cycle. May indicate an off by one cycle execution.");
         cpu.ExecuteCPUCyles(512);
         Assert.AreEqual(0xEBEB, lastReadAddress, "CPU should just keep counting at a rate of 1 per 2 cycles");

         //A reset from the reset pin should only take 6 cpu cycles.
         cpu.ResetCPU();
         cpu.ExecuteCPUCyles(9);
         Assert.AreEqual(0xEAEB, lastReadAddress, "CPU should take 6 cycles to warm reset.");
      }
      [TestMethod]
      public void ArithmeticTest() {
         CPUBasicTestEnviornment.GetNew()
            .WithProgram([
               LDA_Imm, 24,
               STA_ZerX, 0, //24 at 0x0000
               INX,
               ASL_Acc,
               STA_ZerX, 0, //48 at 0x0001
               INX,
               SEC,
               SBC_Abs, 0, 0,
               STA_ZerX, 0, //24 at 0x0002
               INX,
               ADC_Imm, 76,
               LSR_Acc,
               STA_ZerX, 0, //50 at 0x0003
               INX,
               ORA_Imm, 128,
               STA_ZerX, 0, //178 at 0x0004
               INX,
               ADC_Imm, 100,
               STA_ZerX, 0, //23 at 0x0005 (Carry Test)
               INX,
               NOP,
               BCS_Rel, 253, //Infinite loop if carry is set
               LDA_Imm, 1,
               STA_Abs, 05, 00,
               NOP
               ])
            .RunCycles(255)
            .AssertMemoryValue(0x0000, 24, "STA Failed")
            .AssertMemoryValue(0x0001, 48, "ASL Failed")
            .AssertMemoryValue(0x0002, 24, "SBC Failed")
            .AssertMemoryValue(0x0003, 50, "ADC or LSR Failed")
            .AssertMemoryValue(0x0004, 178, "ORA Failed")
            .AssertMemoryValue(0x0005, 23, "Carry Test Failed");
      }
      [TestMethod]
      public void InteruptTest() {
         var t = CPUBasicTestEnviornment.GetNew()
            .WithIRQHandler([
               INX, //2 cycles
               LDA_Imm, 1, //2 cycles
               STA_ZerX, 0, //4 cycles
               RTI //6 cycles
               ])
            .WithNMIHandler([
               INX,
               LDA_Imm, 2,
               STA_ZerX, 0,
               RTI
               ])
            .WithProgram([
               CLI,
               BRK,
               ADC_Imm, 1,//32 INAs
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               ADC_Imm, 1,
               STA_Abs, 0, 1,
               CLC,
               NOP,
               BCC_Rel, 0b11111101 //-3
               ]);
         t.RunCycles(13); //CLI + BRK + INX + LDA_IMM
         t.RunCycles(3);
         t.AssertMemoryValue(0x0001, 0, "Break Instruction Executed Too Early");
         t.RunCycles(1);
         t.AssertMemoryValue(0x0001, 1, "Break Handler took longer than expected");
         t.RunCycles(6); //RTI
         t.RunCycles(12); //Move through some of the INYs
         t.TriggerIRQ(); //Tests NMI overrides
         t.RunCycles(3);
         t.TriggerNMI();
         t.RunCycles(22);
         t.AssertMemoryValue(0x0002, 2, "NMI may not have overriden the IRQ interupt");
         t.RunCycles(7); //Just move through more of the code
         t.TriggerIRQ();
         t.RunCycles(1); //IRQ should not have triggered yet
         t.RunCycles(15);
         t.AssertMemoryValue(0x0003, 1, "IRQ did not trigger on time");
         t.RunCycles(6);
         t.RunCycles(512);
         t.AssertMemoryValue(0x0100, 32, "Interupts interfered with amount of times Y was incrimented");

      }
      [TestMethod]
      public void StackMethodTest() {
         var t = CPUBasicTestEnviornment.GetNew();
         byte val = 0x56;
         t.CPU.PushStackByte(val);
         t.CPU.PushStackUShort(val);
         t.CPU.PopStackUShort();
         Assert.AreEqual(val, t.CPU.PopStackByte(), "CPU can push and pop bytes with no data loss.");
         ushort val2 = 0x9842;
         t.CPU.PushStackUShort(val2);
         t.CPU.PushStackByte(val);
         t.CPU.PopStackByte();
         Assert.AreEqual(val2, t.CPU.PopStackUShort(), "CPU can push and pop ushorts with no data loss.");
      }
   }
}

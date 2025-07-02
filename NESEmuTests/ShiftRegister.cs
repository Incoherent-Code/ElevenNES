using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NESEmu;

namespace NESEmuTests {
   [TestClass]
   public sealed class ShiftRegister {
      [TestMethod]
      public void InvertTest() {
         ShiftRegister8Bit shiftRegister = new();
         shiftRegister.Input = () => 55;
         shiftRegister.PullLatchHIGH();
         shiftRegister.PullLatchLOW();
         int reading = 0;
         for (int i = 0; i < 8; i++) {
            reading = reading << 1;
            reading += shiftRegister.ReadOutput() ? 1 : 0;
            shiftRegister.PulseClock();
         }
         //236 is 55, but backwards in binary, which is what we expect
         Assert.AreEqual(236, reading);
      }
   }
}

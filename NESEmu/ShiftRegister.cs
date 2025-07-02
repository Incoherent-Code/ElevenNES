using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace NESEmu {
   public class ShiftRegister8Bit {
      private bool LatchPinState = false;
      private byte LatchedData = 0;
      public Func<byte> Input { get; set; } = () => 0;
      public void PullLatchLOW() {
         LatchPinState = false;
      }
      public void PullLatchHIGH() {
         if (!LatchPinState) {
            LatchedData = Input();
            LatchPinState = true;
         }
      }
      public bool ReadOutput() {
         return (LatchedData & 1) == 1;
      }
      public void PulseClock() {
         LatchedData = (byte)(LatchedData >> 1);
      }
      /// <summary>
      /// Reads a value, then pulses the clock to incriment to the next value
      /// </summary>
      public bool ReadWithPulse() {
         var value = ReadOutput();
         PulseClock();
         return value;
      }
   }
}

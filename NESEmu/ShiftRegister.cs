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
      /// Simulates an NES IO Register mapped to a shift register. (As seen in controller IO).
      /// Reading will automatically read the value and shift the shift register.
      /// Writing will write to the "Latch pin" of the shift register.
      /// </summary>
      public byte IORegister { get {
            var output = (byte)(LatchedData & 1);
            PulseClock();
            return output;
         } 
         set {
            if ((value & 1) == 1)
               PullLatchHIGH();
            else 
               PullLatchLOW();
         } }
   }
}

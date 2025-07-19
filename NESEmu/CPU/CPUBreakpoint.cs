using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESEmu.CPU {
   public struct CPUBreakpoint(int address) {
      public ushort Address = (ushort)address;
      public bool OnRead = false;
      public bool OnWrite = false;
      public bool OnExecute = true;
   }
}

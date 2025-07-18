using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESEmu.CPU {
   public class CPUException(string message, byte opcode, ushort location) : Exception(message) {
      public byte OPCode = opcode;
      public ushort Location = location;
#if DEBUG
      public string OpCodeName = _6502OPCode.GetOpCode(opcode);
#endif
   }
}

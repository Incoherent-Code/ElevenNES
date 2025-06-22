using NESEmu.Rom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NESEmu.Mappers {
   public static class MapperFactory {
      public static Mapper CreateMapper(iNESHeader header, FileStream file) {
         return header.MapperID switch {
            0 => new Mapper000_NROM(header, file),

            _ => throw new NotSupportedException($"Mapper {header.MapperID} is not currently supported.")
         };
      }
   }
}

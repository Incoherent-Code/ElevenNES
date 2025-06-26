using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElevenNES {
   public class Config {
      private static Config _instance = new();
      public static Config Instance => _instance;
      //Constants (Not actually Configurable)


      //Any actual parameters need to be non-static
      public int Scale = 3;
      public static void Load(string path) {
         _instance = JsonSerializer.Deserialize<Config>(File.ReadAllText(path));
      }
   }
}

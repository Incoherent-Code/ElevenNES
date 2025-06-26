using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ElevenNES.UI {
   public class Dropdown : ContainerRuntime {
      public const int DropdownEntryHeight = 10;
      private ColoredRectangleRuntime BGRect = new();
      public Color BackgroundColor { get => BGRect.Color; set => BGRect.Color = value; }
      public Dropdown(DropdownEntryInfo[] Entries) {
         BackgroundColor = Color.DarkBlue;

         this.Visible = false;
         this.Width = 60 * Config.Instance.Scale;
         this.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
         this.Height = DropdownEntryHeight * Config.Instance.Scale * Entries.Length;

         BGRect.Dock(Gum.Wireframe.Dock.Fill);
         AddChild(BGRect);

         for (int i = 0; i < Entries.Length; i++) {
            var DEntry = new DropdownEntry(Entries[i], this);
            DEntry.X = 0;
            DEntry.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            DEntry.Y = DropdownEntryHeight * i * Config.Instance.Scale;
            DEntry.Dock(Gum.Wireframe.Dock.FillHorizontally);
            AddChild(DEntry);
         }

      }
   }
}

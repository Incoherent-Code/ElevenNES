using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevenNES.UI {
   public struct DropdownEntryInfo {
      public string Name;
      public Keys Keybind;
      public Action Action;
      public DropdownEntryInfo(string name, Keys keybind, Action action) {
         Name = name;
         Keybind = keybind;
         Action = action;
      }
   }
   public class DropdownEntry : ContainerRuntime {
      private ColoredRectangleRuntime _bgRect = new();
      private TextRuntime _text = new();
      private DropdownEntryInfo _entry;
      private Dropdown _parent;
      public Color SelectColor {  get => _bgRect.Color; set => _bgRect.Color = value; }


      public DropdownEntry(DropdownEntryInfo info, Dropdown parent) {
         SelectColor = Color.YellowGreen;

         _entry = info;
         _parent = parent;

         Height = Dropdown.DropdownEntryHeight * Config.Instance.Scale;

         _bgRect.Visible = false;
         _bgRect.Dock(Gum.Wireframe.Dock.Fill);
         AddChild(_bgRect);

         _text.Color = Color.White;
         _text.Anchor(Gum.Wireframe.Anchor.Left);
         _text.Text = info.Name;
         _text.UseCustomFont = true;
         _text.CustomFontFile = "Fonts/QuanPixel-standard.fnt";
         _text.FontScale = Config.Instance.Scale;
         AddChild(_text);

         this.RollOn += (_, _) => {
            _bgRect.Visible = true;
         };

         this.RollOff += (_, _) => {
            _bgRect.Visible = false;
         };

         this.Click += (_, _) => {
            _entry.Action();
            _parent.Visible = false;
         };
      }
   }
}

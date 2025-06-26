using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.GueDeriving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevenNES.UI {
   public class TopButton : Button {
      private const float ButtonHeight = 12;
      private ContainerRuntime MainContainer = new ContainerRuntime();
      private TextRuntime TextInstance = new TextRuntime();
      /// <summary>
      /// Primary Color of the button
      /// </summary>
      private ColoredRectangleRuntime ForegroundRect = new ColoredRectangleRuntime();
      /// <summary>
      /// Secondary Color of the button. Forms like a shadow. Secondary Color.
      /// </summary>
      private ColoredRectangleRuntime BackgroundRect = new ColoredRectangleRuntime();
      public Color PrimaryColor { get; set; } = new Color(66, 66, 66);
      public Color AccentColor { get => this.BackgroundRect.Color; set => this.BackgroundRect.Color = value; }
      public TopButton(string text) {
         AccentColor = new Color(44, 44, 44);

         TextInstance.Text = text;
         //This has something to do with hooking into Button.Text
         TextInstance.Name = "TextInstance";
         TextInstance.UseCustomFont = true;
         TextInstance.CustomFontFile = "Fonts/QuanPixel-standard.fnt";
         TextInstance.FontScale = Config.Instance.Scale;
         TextInstance.Color = new Color(195, 195, 195);
         TextInstance.Anchor(Gum.Wireframe.Anchor.Center);

         BackgroundRect.Height = (ButtonHeight - 1) * Config.Instance.Scale;
         BackgroundRect.Anchor(Gum.Wireframe.Anchor.BottomRight);
         BackgroundRect.Width = TextInstance.GetAbsoluteWidth() + 2 * Config.Instance.Scale;
         MainContainer.AddChild(BackgroundRect);

         ForegroundRect.Height = (ButtonHeight - 1) * Config.Instance.Scale;
         ForegroundRect.Anchor(Gum.Wireframe.Anchor.TopLeft);
         ForegroundRect.Width = TextInstance.GetAbsoluteWidth() + 2 * Config.Instance.Scale;
         ForegroundRect.Color = PrimaryColor;
         MainContainer.AddChild(ForegroundRect);
         MainContainer.RollOn += (_, _) => ForegroundRect.Color = AccentColor;
         MainContainer.RollOff += (_, _) => ForegroundRect.Color = PrimaryColor;


         MainContainer.Height = ButtonHeight * Config.Instance.Scale;
         MainContainer.Width = TextInstance.GetAbsoluteWidth() + 3 * Config.Instance.Scale;
         MainContainer.AddChild(TextInstance);

         this.Visual = MainContainer;
      }
   }
}

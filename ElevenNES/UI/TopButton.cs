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
      private const float ButtonHeight = 14;
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
      public Color PrimaryColor { get => this.ForegroundRect.Color; set => this.ForegroundRect.Color = value; }
      public Color AccentColor { get => this.BackgroundRect.Color; set => this.BackgroundRect.Color = value; }
      public TopButton(string text) {
         TextInstance.Text = text;
         //This has something to do with hooking into Button.Text
         TextInstance.Name = "TextInstance";
         TextInstance.UseCustomFont = true;
         TextInstance.CustomFontFile = "Fonts/QuanPixel-standard.fnt";
         TextInstance.FontScale = ElevenNES.Scale;
         TextInstance.Anchor(Gum.Wireframe.Anchor.Center);
         ForegroundRect.AddChild(TextInstance);

         BackgroundRect.Height = (ButtonHeight - 1) * ElevenNES.Scale;
         BackgroundRect.Anchor(Gum.Wireframe.Anchor.BottomRight);
         BackgroundRect.Width = TextInstance.GetAbsoluteWidth() + 2 * ElevenNES.Scale;
         MainContainer.AddChild(BackgroundRect);

         ForegroundRect.Height = (ButtonHeight - 1) * ElevenNES.Scale;
         ForegroundRect.Anchor(Gum.Wireframe.Anchor.TopLeft);
         ForegroundRect.Width = TextInstance.GetAbsoluteWidth() + 2 * ElevenNES.Scale;
         MainContainer.AddChild(ForegroundRect);
         this.GotFocus += (_, _) => ForegroundRect.Visible = false;
         this.LostFocus += (_, _) => ForegroundRect.Visible = true;


         MainContainer.Height = ButtonHeight * ElevenNES.Scale;
         MainContainer.Width = TextInstance.GetAbsoluteWidth() + 3 * ElevenNES.Scale;

         PrimaryColor = Color.DarkGray;
         AccentColor = new Color(100, 100, 100);

         this.Visual = MainContainer;
      }
   }
}

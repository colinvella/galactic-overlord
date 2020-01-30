using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace GalacticOverlord
{
    public class GraphicsUtility
    {
        public static void DrawOutlinedText(SpriteBatch spriteBatch, SpriteFont spriteFont, Vector2 position, string text, Color colour)
        {
            Color outlineColour = Color.Black;
            outlineColour.A = colour.A;

            position.X += 1.0f;
            spriteBatch.DrawString(spriteFont, text, position, outlineColour);

            position.Y += 2.0f;
            spriteBatch.DrawString(spriteFont, text, position, outlineColour);

            position.X -= 1.0f;
            position.Y -= 1.0f;
            spriteBatch.DrawString(spriteFont, text, position, outlineColour);

            position.X += 2.0f;
            spriteBatch.DrawString(spriteFont, text, position, outlineColour);

            position.X -= 1.0f;
            spriteBatch.DrawString(spriteFont, text, position, colour);
        }

        public static void DrawOutlinedText(SpriteBatch spriteBatch, SpriteFont spriteFont, Vector2 position, string text, Color colour, TextAlignment textAlignment)
        {
            if (textAlignment != TextAlignment.TopLeft)
            {
                Vector2 textSize = spriteFont.MeasureString(text);
                textSize.X += 2.0f; textSize.Y += 2.0f;

                switch (textAlignment)
                {
                    case TextAlignment.Top:
                        position.X -= textSize.X * 0.5f;
                        break;
                    case TextAlignment.TopRight:
                        position.X -= textSize.X;
                        break;
                    case TextAlignment.Centre:
                        position -= textSize * 0.5f;
                        break;
                    case TextAlignment.Right:
                        position.X -= textSize.X;
                        position.Y -= textSize.Y * 0.5f;
                        break;
                    case TextAlignment.BottomLeft:
                        position.Y -= textSize.Y;
                        break;
                    case TextAlignment.Bottom:
                        position.X -= textSize.X * 0.5f;
                        position.Y -= textSize.Y;
                        break;
                    case TextAlignment.BottomRight:
                        position -= textSize;
                        break;
                }
            }

            DrawOutlinedText(spriteBatch, spriteFont, position, text, colour);
        }

        public static IEnumerable<string> WrapText(SpriteFont spriteFont, float width, string text)
        {
            List<string> lines = new List<string>();
            if (text.Length < 2 || spriteFont.MeasureString(text).X <= width)
            {
                lines.Add(text);
                return lines;
            }

            int wrapIndex = 0;
            while (true)
            {
                int nextWrapIndex = text.IndexOf(' ', wrapIndex + 1);
                if (nextWrapIndex == -1)
                    nextWrapIndex = text.Length;

                if (spriteFont.MeasureString(text.Substring(0, nextWrapIndex)).X > width)
                {
                    ++wrapIndex; //skip space for next line
                    break;
                }

                wrapIndex = nextWrapIndex;
            }

            string firstLine = text.Substring(0, wrapIndex);
            string remainingLines = text.Substring(wrapIndex);

            lines.Add(firstLine);
            lines.AddRange(WrapText(spriteFont, width, remainingLines));

            return lines;
        }
    }

    public enum TextAlignment
    {
        TopLeft,
        Top,
        TopRight,
        Left,
        Centre,
        Right,
        BottomLeft,
        Bottom,
        BottomRight
    }
}

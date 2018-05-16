using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// Heavily based on https://github.com/libgdx/libgdx/blob/master/gdx/src/com/badlogic/gdx/scenes/scene2d/ui/TextArea.java

namespace Nez.UI
{
    public class TextArea :TextField
    {
        List<int> linesBreak = new List<int>();

        /** Current line for the cursor **/
        int _cursorLine = 0;

        /** Last text processed. This attribute is used to avoid unnecessary computations while calculating offsets **/
        private String lastText;

        /** Index of the first line showed by the text area **/
        public int firstLineShowing = 0;

        /** Number of lines showed by the text area **/
        private int linesShowing = 2;

        public override float preferredHeight
        {
            get
            {
                if (_preferredRows <= 0)
                { return base.preferredHeight; }
                else
                {
                    return textHeight * _preferredRows;
                }
            }
        }


        float _preferredRows = 0;
        public TextArea()
        { }
        public TextArea(string text,TextFieldStyle style)
        {
            // do not call base constructor because we need writeenters to be true
            writeEnters = true;
            setStyle(style);
            setText(text);
            setSize(preferredWidth,preferredHeight);

        }

        public TextArea(string text,Skin skin,string styleName = null)
        {
            // do not call base constructor because we need writeenters to be true
            writeEnters = true;
            setStyle(skin.get<TextFieldStyle>(styleName));
            setText(text);
            setSize(preferredWidth,preferredHeight);

        }

        #region Configuration

        public TextField setPreferredRows(float preferredRows)
        {
            _preferredRows = preferredRows;
            return this;
        }

        #endregion

        #region Drawing
        public override void draw(Graphics graphics,float parentAlpha)
        {
            var font = style.font;
            var fontColor = (disabled && style.disabledFontColor.HasValue) ? style.disabledFontColor.Value
                : ((_isFocused && style.focusedFontColor.HasValue) ? style.focusedFontColor.Value : style.fontColor);
            IDrawable selection = style.selection;
            IDrawable background = (disabled && style.disabledBackground != null) ? style.disabledBackground
                : ((_isFocused && style.focusedBackground != null) ? style.focusedBackground : style.background);

            var color = getColor();
            var x = getX();
            var y = getY();
            var width = getWidth();
            var height = getHeight();

            float bgLeftWidth = 0, bgRightWidth = 0;
            if (background != null)
            {
                background.draw(graphics,x,y,width,height,new Color(color,(int)(color.A * parentAlpha)));
                bgLeftWidth = background.leftWidth;
                bgRightWidth = background.rightWidth;
            }

            var textY = getTextY(font,background);
            var yOffset = (textY < 0) ? -textY - font.lineHeight / 2f + getHeight() / 2 : 0;
            calculateOffsets();

            if (_isFocused && hasSelection && selection != null)
                drawSelection(selection,graphics,font,x + bgLeftWidth,y + textY + yOffset);

            if (displayText.Length == 0)
            {
            }
            else
            {
                var col = new Color(fontColor,(int)(fontColor.A * parentAlpha / 255.0f));
                // var t = displayText.Substring(visibleTextStart,visibleTextEnd - visibleTextStart);

                float lineOffset = 0;
                for (int i = firstLineShowing * 2; i < (firstLineShowing + linesShowing) * 2 && i < linesBreak.Count; i += 2)
                {
                    //font.draw(batch,displayText,x,y + offsetY,linesBreak.items[i],linesBreak.items[i + 1],0,Align.left,false);
                    var t = displayText.Substring(linesBreak[i],linesBreak[i + 1]);
                    graphics.batcher.drawString(font,t,new Vector2(x + bgLeftWidth + textOffset,y + textY + yOffset + lineOffset),col);

                    lineOffset += font.lineHeight;
                }
            }

            if (_isFocused && !disabled)
            {
                blink();
                if (cursorOn && style.cursor != null)
                    drawCursor(style.cursor,graphics,font,x + bgLeftWidth,y + textY + yOffset);
            }
        }
        #endregion

        protected override void calculateOffsets()
        {
            base.calculateOffsets();
            if (this.text != lastText)
            {
                this.lastText = text;
                var font = style.font;

                linesBreak.Clear();
                int lineStart = 0;
                int lastSpace = 0;
                char lastCharacter;
                for (int i = 0; i < text.Length; i++)
                {
                    lastCharacter = text[i];
                    if (lastCharacter == ENTER_DESKTOP || lastCharacter == ENTER_ANDROID)
                    {
                        linesBreak.Add(lineStart);
                        linesBreak.Add(i);
                        lineStart = i + 1;
                    }
                    else
                    {
                        lastSpace = (continueCursor(i,0) ? lastSpace : i);
                    }
                }
                // Add last line
                if (lineStart < text.Length)
                {
                    linesBreak.Add(lineStart);
                    linesBreak.Add(text.Length);
                }
                // showCursor();

            }
        }

        protected override void sizeChanged()
        {
            lastText = null; // Cause calculateOffsets to recalculate the line breaks.

            // The number of lines showed must be updated whenever the height is updated
            /*
            BitmapFont font = style.font;
            Drawable background = style.background;
            float availableHeight = getHeight() - (background == null ? 0 : background.getBottomHeight() + background.getTopHeight());
            linesShowing = (int)Math.floor(availableHeight / font.getLineHeight());
            */
        }
    }
}

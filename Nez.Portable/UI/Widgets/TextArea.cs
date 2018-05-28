using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez.BitmapFonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// Heavily based on https://github.com/libgdx/libgdx/blob/master/gdx/src/com/badlogic/gdx/scenes/scene2d/ui/TextArea.java

namespace Nez.UI
{
    public class TextArea :TextField, IKeyboardListener
    {
        List<int> linesBreak = new List<int>();

        /** Current line for the cursor **/
        int _cursorLine = 0;

        /** Variable to maintain the x offset of the cursor when moving up and down. If it's set to -1, the offset is reset **/
        float _moveOffset = -1;
        /** Last text processed. This attribute is used to avoid unnecessary computations while calculating offsets **/
        private String lastText;

        /** Index of the first line showed by the text area **/
        public int firstLineShowing = 0;

        /** Number of lines showed by the text area **/
        private int linesShowing = 0;

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
                drawSelection(selection,graphics,font,x + bgLeftWidth,y + yOffset);

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
                    var t = displayText.Substring(linesBreak[i],linesBreak[i + 1] - linesBreak[i]);
                    graphics.batcher.drawString(font,t,new Vector2(x + bgLeftWidth + textOffset,y + yOffset + lineOffset),col);

                    lineOffset += font.lineHeight;
                }
            }

            if (_isFocused && !disabled)
            {
                blink();
                if (cursorOn && style.cursor != null)
                    drawCursor(style.cursor,graphics,font,x + bgLeftWidth,y + yOffset);
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
                showCursor();

            }
        }

        public override TextField setSelection(int selectionStart,int selectionEnd)
        {
            base.setSelection(selectionStart,selectionEnd);
            updateCurrentLine();
            return this;
        }

        protected override void setCursorPosition(float x,float y)
        {
            _moveOffset = -1;

            IDrawable background = style.background;
            BitmapFont font = style.font;

            float height = getHeight();

            if (background != null)
            {
                height -= background.topHeight;
                x -= background.leftWidth;
            }
            x = Math.Max(0,x);
            if (background != null)
            {
                y -= background.topHeight;
            }

            _cursorLine = (int)Math.Floor((height - (height - y)) / font.lineHeight) + firstLineShowing;
            _cursorLine = Math.Max(0,Math.Min(_cursorLine,getLines() + 1));

            base.setCursorPosition(x,y);
            updateCurrentLine();
        }

        protected override bool continueCursor(int index,int offset)
        {
            int pos = calculateCurrentLineIndex(index + offset);
            return base.continueCursor(index,offset) && (pos < 0 || pos >= linesBreak.Count - 2 || (linesBreak[pos + 1] != index)
                || (linesBreak[pos + 1] == linesBreak[pos + 2]));
        }

        private void showCursor()
        {
            updateCurrentLine();
            updateFirstLineShowing();
        }

        private void updateFirstLineShowing()
        {
            if (_cursorLine != firstLineShowing)
            {
                int step = _cursorLine >= firstLineShowing ? 1 : -1;
                while (firstLineShowing > _cursorLine || firstLineShowing + linesShowing - 1 < _cursorLine)
                {
                    firstLineShowing += step;
                }
            }
        }

        /** Calculates the text area line for the given cursor position **/
        private int calculateCurrentLineIndex(int cursor)
        {
            int index = 0;
            while (index < linesBreak.Count && cursor > linesBreak[index])
            {
                index++;
            }
            return index;
        }

        protected override void moveCursor(bool forward,bool jump)
        {
            int count = forward ? 1 : -1;
            int index = (_cursorLine * 2) + count;
            if (index >= 0 && index + 1 < linesBreak.Count && linesBreak[index] == cursor
                && linesBreak[index + 1] == cursor)
            {
                _cursorLine += count;
                if (jump)
                {
                    base.moveCursor(forward,jump);
                }
                showCursor();
            }
            else
            {
                base.moveCursor(forward,jump);
            }
            updateCurrentLine();
        }

        /** Updates the current line, checking the cursor position in the text **/
        private void updateCurrentLine()
        {
            int index = calculateCurrentLineIndex(cursor);
            int line = index / 2;
            // Special case when cursor moves to the beginning of the line from the end of another and a word
            // wider than the box
            if (index % 2 == 0 || index + 1 >= linesBreak.Count || cursor != linesBreak[index]
                || linesBreak[index + 1] != linesBreak[index])
            {
                if (line < linesBreak.Count / 2 || text.Length == 0 || text[text.Length - 1] == ENTER_ANDROID
                    || text[text.Length - 1] == ENTER_DESKTOP)
                {
                    _cursorLine = line;
                }
            }
            updateFirstLineShowing();   // fix for drag-selecting text out of the TextArea's bounds
        }
        protected override void sizeChanged()
        {
            lastText = null; // Cause calculateOffsets to recalculate the line breaks.

            // The number of lines showed must be updated whenever the height is updated

            BitmapFont font = style.font;
            IDrawable background = style.background;
            float availableHeight = getHeight() - (background == null ? 0 : background.bottomHeight + background.topHeight);
            linesShowing = (int)Math.Floor(availableHeight / font.lineHeight);

        }

        void IKeyboardListener.keyDown(Keys key)
        {
            OnKeyDown(key);
            showCursor();
        }

        /** Returns total number of lines that the text occupies **/
        public int getLines()
        {
            return linesBreak.Count / 2 + (newLineAtEnd() ? 1 : 0);
        }

        /** Returns if there's a new line at then end of the text **/
        public bool newLineAtEnd()
        {
            return text.Length != 0
                && (text[text.Length - 1] == ENTER_ANDROID || text[text.Length - 1] == ENTER_DESKTOP);
        }

        /** Moves the cursor to the given number line **/
        public void moveCursorLine(int line)
        {
            if (line < 0)
            {
                _cursorLine = 0;
                cursor = 0;
                _moveOffset = -1;
            }
            else if (line >= getLines())
            {
                int newLine = getLines() - 1;
                cursor = text.Length;
                if (line > getLines() || newLine == _cursorLine)
                {
                    _moveOffset = -1;
                }
                _cursorLine = newLine;
            }
            else if (line != _cursorLine)
            {
                if (_moveOffset < 0)
                {
                    _moveOffset = linesBreak.Count <= _cursorLine * 2 ? 0
                        : glyphPositions[cursor] - glyphPositions[linesBreak[_cursorLine * 2]];
                }
                _cursorLine = line;
                cursor = _cursorLine * 2 >= linesBreak.Count ? text.Length : linesBreak[_cursorLine * 2];
                while (cursor < text.Length && cursor <= linesBreak[_cursorLine * 2 + 1] - 1
                    && glyphPositions[cursor] - glyphPositions[linesBreak[_cursorLine * 2]] < _moveOffset)
                {
                    cursor++;
                }
                showCursor();
            }
        }

        protected override void OnKeyDown(Keys key)
        {
            base.OnKeyDown(key);

            if (key == Keys.Up)
            {
                moveCursorLine(_cursorLine - 1);
            }
            else if (key == Keys.Down)
            {
                moveCursorLine(_cursorLine + 1);
            }
        }

        protected override void drawSelection(IDrawable selection,Graphics graphics,BitmapFont font,float x,float y)
        {



            int i = firstLineShowing * 2;
            float offsetY = 0;
            int minIndex = Math.Min(cursor,selectionStart);
            int maxIndex = Math.Max(cursor,selectionStart);
            while (i + 1 < linesBreak.Count && i < (firstLineShowing + linesShowing) * 2)
            {

                int lineStart = linesBreak[i];
                int lineEnd = linesBreak[i + 1];

                if (!((minIndex < lineStart && minIndex < lineEnd && maxIndex < lineStart && maxIndex < lineEnd)
                    || (minIndex > lineStart && minIndex > lineEnd && maxIndex > lineStart && maxIndex > lineEnd)))
                {

                    int start = Math.Max(linesBreak[i],minIndex);
                    int end = Math.Min(linesBreak[i + 1],maxIndex);

                    float selectionX = glyphPositions[start] - glyphPositions[linesBreak[i]];
                    float selectionWidth = glyphPositions[end] - glyphPositions[start];

                    //selection.draw(batch,x + selectionX + fontOffset,y - textHeight - font.getDescent() - offsetY,selectionWidth,
                    //    font.getLineHeight());

                    selection.draw(graphics,x + selectionX + renderOffset + fontOffset,y - font.descent / 2,selectionWidth,textHeight,Color.White);
                }

                offsetY += font.lineHeight;
                i += 2;
            }
        }


        protected override void drawCursor(IDrawable cursorPatch,Graphics graphics,BitmapFont font,float x,float y)
        {
            float tempOffset;
            if (cursor >= glyphPositions.Count || _cursorLine * 2 >= linesBreak.Count)
                tempOffset = 0;
            else
                tempOffset = glyphPositions[cursor] - glyphPositions[linesBreak[_cursorLine * 2]];

            //cursorPatch.draw(graphics,
            //    x + tempOffset + glyphPositions[cursor] - glyphPositions[visibleTextStart] + fontOffset - 1 /*font.getData().cursorX*/,
            //    y - font.descent / 2,cursorPatch.minWidth,textHeight,color);

            cursorPatch.draw(graphics,x + tempOffset + fontOffset - 1 /*+ font.getData().cursorX*/,
            y - font.descent / 2 + (_cursorLine - firstLineShowing) * font.lineHeight,cursorPatch.minWidth,
            font.lineHeight,color);

        }

        protected override float getTextY(BitmapFont font,IDrawable background)
        {
            float height = getHeight();
            float textY = textHeight / 2 + font.descent;
            if (background != null)
            {
                float bottom = background.bottomHeight;
                textY = textY + (height - background.topHeight - bottom) / 2 + bottom;
            }
            else
            {
                textY = textY + height / 2;
            }
            //todo check this if (font.usesIntegerPositions())
            //    textY = (int)textY;
            return textY;
        }

        protected override void goHome()
        {
            if (false)
            {
                cursor = 0;
            }
            else if (_cursorLine * 2 < linesBreak.Count)
            {
                cursor = linesBreak[_cursorLine * 2];
            }
        }


        protected override void goEnd()
        {
            if (_cursorLine >= getLines())
            {
                cursor = text.Length;
            }
            else if (_cursorLine * 2 + 1 < linesBreak.Count)
            {
                cursor = linesBreak[_cursorLine * 2 + 1];
            }
        }
    }
}

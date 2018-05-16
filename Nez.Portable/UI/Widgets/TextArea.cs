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
        /** Current line for the cursor **/
        int _cursorLine = 0;

        /** Last text processed. This attribute is used to avoid unnecessary computations while calculating offsets **/
        private String lastText;

        /** Index of the first line showed by the text area **/
        public int firstLineShowing = 0;

        /** Number of lines showed by the text area **/
        private int linesShowing;

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

        public TextArea(string text,TextFieldStyle style) : base(text,style)
        {

        }

        public TextArea(string text,Skin skin,string styleName = null) : base(text,skin,styleName)
        {

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
            base.draw(graphics,parentAlpha);
        }
        #endregion

        protected override void calculateOffsets()
        {
            base.calculateOffsets();
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

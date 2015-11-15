﻿//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
//
// C# Port port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007-2011
//
// Permission to copy, use, modify, sell and distribute this software 
// is granted provided this copyright notice appears in all copies. 
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
//
// Class StringPrinter.cs
// 
// Class to output the vertex source of a string as a run of glyphs.
//----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

using MatterHackers.Agg;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;

namespace MatterHackers.Agg.Font
{
    internal enum Justification { Left, Center, Right };
    internal enum Baseline { BoundsTop, BoundsCenter, TextCenter, Text, BoundsBottom };

    internal class StringPrinter : IVertexSource
    {
        int currentChar;
        Vector2 currentOffset;
        IVertexSource currentGlyph;

        StyledTypeFace typeFaceStyle;

        String text = "";

        Vector2 totalSizeCach;

        public Justification Justification { get; set; }
        public Baseline Baseline { get; set; }

        public StyledTypeFace TypeFaceStyle
        {
            get
            {
                return typeFaceStyle;
            }
        }

        public String Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
            }
        }

        public Vector2 Origin { get; set; }


        public StringPrinter(String text = "", double pointSize = 12, Vector2 origin = new Vector2(), Justification justification = Justification.Left, Baseline baseline = Baseline.Text)
            : this(text, new StyledTypeFace(LiberationSansFont.Instance, pointSize), origin, justification, baseline)
        {
        }
        
        public StringPrinter(String text, StyledTypeFace typeFaceStyle, Vector2 origin = new Vector2(), Justification justification = Justification.Left, Baseline baseline = Baseline.Text)
        {
            this.typeFaceStyle = typeFaceStyle;
            this.text = text;
            this.Justification = justification;
            this.Origin = origin;
            this.Baseline = baseline;
        }

        public StringPrinter(String text, StringPrinter copyPropertiesFrom)
            : this(text, copyPropertiesFrom.TypeFaceStyle, copyPropertiesFrom.Origin, copyPropertiesFrom.Justification, copyPropertiesFrom.Baseline)
        {
        }

        public RectangleDouble LocalBounds
        {
            get
            {
                Vector2 size = GetSize();
                RectangleDouble bounds;

                switch (Justification)
                {
                    case Justification.Left:
                        bounds = new RectangleDouble(0, typeFaceStyle.DescentInPixels, size.x, size.y + typeFaceStyle.DescentInPixels);
                        break;

                    case Justification.Center:
                        bounds = new RectangleDouble(-size.x / 2, typeFaceStyle.DescentInPixels, size.x / 2, size.y + typeFaceStyle.DescentInPixels);
                        break;

                    case Justification.Right:
                        bounds = new RectangleDouble(-size.x, typeFaceStyle.DescentInPixels, 0, size.y + typeFaceStyle.DescentInPixels);
                        break;

                    default:
                        throw new NotImplementedException();
                }

                bounds.Offset(Origin);
                return bounds;
            }
        }

        public void rewind(int pathId)
        {
            currentChar = 0;
            currentOffset = new Vector2(0, 0);
            if (text != null && text.Length > 0)
            {
                currentGlyph = typeFaceStyle.GetGlyphForCharacter(text[currentChar]);
                if (currentGlyph != null)
                {
                    currentGlyph.rewind(0);
                }
            }
        }

        public Path.FlagsAndCommand vertex(out double x, out double y)
        {
            x = 0;
            y = 0;
            if (text != null && text.Length > 0)
            {
                Path.FlagsAndCommand curCommand = Path.FlagsAndCommand.CommandStop;
                if (currentGlyph != null)
                {
                    curCommand = currentGlyph.vertex(out x, out y);
                }

                double xAlignOffset = 0;
                Vector2 size = GetSize();
                switch (Justification)
                {
                    case Justification.Left:
                        xAlignOffset = 0;
                        break;

                    case Justification.Center:
                        xAlignOffset = -size.x / 2;
                        break;

                    case Justification.Right:
                        xAlignOffset = -size.x;
                        break;

                    default:
                        throw new NotImplementedException();
                }

                double yAlignOffset = 0;
                switch (Baseline)
                {
                    case Baseline.Text:
                        yAlignOffset = 0;
                        break;

                    case Baseline.BoundsTop:
                        yAlignOffset = -typeFaceStyle.AscentInPixels;
                        break;
                        
                    case Baseline.BoundsCenter:
                        yAlignOffset = -typeFaceStyle.AscentInPixels/2;
                        break;

                    default:
                        throw new NotImplementedException();
                }

                
                while (curCommand == Path.FlagsAndCommand.CommandStop
                    && currentChar < text.Length - 1)
                {
                    if (currentChar < text.Length)
                    {
                        // pass the next char so the typeFaceStyle can do kerning if it needs to.
                        currentOffset.x += typeFaceStyle.GetAdvanceForCharacter(text[currentChar], text[currentChar + 1]);
                    }
                    else
                    {
                        currentOffset.x += typeFaceStyle.GetAdvanceForCharacter(text[currentChar]);
                    }

                    currentChar++;
                    currentGlyph = typeFaceStyle.GetGlyphForCharacter(text[currentChar]);
                    if (currentGlyph != null)
                    {
                        currentGlyph.rewind(0);
                        curCommand = currentGlyph.vertex(out x, out y);
                    }
                    else if (text[currentChar] == '\n')
                    {
                        if (currentChar + 1 < text.Length - 1 && (text[currentChar + 1] == '\n') && text[currentChar] != text[currentChar + 1])
                        {
                            currentChar++;
                        }
                        currentOffset.x = 0;
                        currentOffset.y -= typeFaceStyle.EmSizeInPixels;
                    }
                }

                x += currentOffset.x + xAlignOffset + Origin.x;
                y += currentOffset.y + yAlignOffset + Origin.y;

                return curCommand;
            }

            return Path.FlagsAndCommand.CommandStop;
        }

        public void DrawFromHintedCache(Graphics2D graphics2D, RGBA_Bytes color)
        {
            // TODO: actually make this draw from the hinted cache.
            graphics2D.Render(this, color);
        }

        public Vector2 GetSize()
        {
            if (totalSizeCach.x == 0)
            {
                Vector2 offset;
                GetSize(0, Math.Max(0, text.Length - 1), out offset);
                totalSizeCach = offset;
            }

            return totalSizeCach;
        }

        public void GetSize(int characterToMeasureStartIndexInclusive, int characterToMeasureEndIndexInclusive, out Vector2 offset)
        {
            offset.x = 0;
            offset.y = typeFaceStyle.EmSizeInPixels;

            double currentLineX = 0;

            for (int i = characterToMeasureStartIndexInclusive; i < characterToMeasureEndIndexInclusive; i++)
            {
                if (text[i] == '\n')
                {
                    if (i + 1 < characterToMeasureEndIndexInclusive && (text[i + 1] == '\n') && text[i] != text[i+1])
                    {
                        i++;
                    }
                    currentLineX = 0;
                    offset.y += typeFaceStyle.EmSizeInPixels;
                }
                else
                {
                    currentLineX += typeFaceStyle.GetAdvanceForCharacter(text[i], text[i + 1]);
                    if (currentLineX > offset.x)
                    {
                        offset.x = currentLineX;
                    }
                }
            }

            if (text.Length > characterToMeasureEndIndexInclusive)
            {
                offset.x += typeFaceStyle.GetAdvanceForCharacter(text[characterToMeasureEndIndexInclusive]);
            }
        }

        public int NumLines()
        {
            int characterToMeasureStartIndexInclusive = 0;
            int characterToMeasureEndIndexInclusive = text.Length - 1;
            return NumLines(characterToMeasureStartIndexInclusive, characterToMeasureEndIndexInclusive);
        }

        public int NumLines(int characterToMeasureStartIndexInclusive, int characterToMeasureEndIndexInclusive)
        {
            int numLines = 1;
            
            characterToMeasureStartIndexInclusive = Math.Max(0, Math.Min(characterToMeasureStartIndexInclusive, text.Length - 1));
            characterToMeasureEndIndexInclusive = Math.Max(0, Math.Min(characterToMeasureEndIndexInclusive, text.Length - 1));
            for (int i = characterToMeasureStartIndexInclusive; i < characterToMeasureEndIndexInclusive; i++)
            {
                if (text[i] == '\n')
                {
                    if (i + 1 < characterToMeasureEndIndexInclusive && (text[i + 1] == '\n') && text[i] != text[i + 1])
                    {
                        i++;
                    }
                    numLines++;
                }
            }

            return numLines;
        }

        public void GetOffset(int characterToMeasureStartIndexInclusive, int characterToMeasureEndIndexInclusive, out Vector2 offset)
        {
            offset = Vector2.Zero;

            characterToMeasureEndIndexInclusive = Math.Min(text.Length-1, characterToMeasureEndIndexInclusive);

            for (int index = characterToMeasureStartIndexInclusive; index <= characterToMeasureEndIndexInclusive; index++)
            {
                if (text[index] == '\n')
                {
                    offset.x = 0;
                    offset.y -= typeFaceStyle.EmSizeInPixels;
                }
                else
                {
                    if (index < text.Length - 1)
                    {
                        offset.x += typeFaceStyle.GetAdvanceForCharacter(text[index], text[index + 1]);
                    }
                    else
                    {
                        offset.x += typeFaceStyle.GetAdvanceForCharacter(text[index]);
                    }
                }
            }
        }

        // this will return the position to the left of the requested character.
        public Vector2 GetOffsetLeftOfCharacterIndex(int characterIndex)
        {
            Vector2 offset;
            GetOffset(0, characterIndex - 1, out offset);
            return offset;
        }

        // If the Text is "TEXT" and the position is less than half the distance to the center
        // of "T" the return value will be 0 if it is between the center of 'T' and the center of 'E'
        // it will be 1 and so on.
        public int GetCharacterIndexToStartBefore(Vector2 position)
        {
            int clostestIndex = -1;
            double clostestXDistSquared = double.MaxValue;
            double clostestYDistSquared = double.MaxValue;
            Vector2 offset = new Vector2(0, typeFaceStyle.EmSizeInPixels * NumLines());
            int characterToMeasureStartIndexInclusive = 0;
            int characterToMeasureEndIndexInclusive = text.Length - 1;
            if (text.Length > 0)
            {
                characterToMeasureStartIndexInclusive = Math.Max(0, Math.Min(characterToMeasureStartIndexInclusive, text.Length - 1));
                characterToMeasureEndIndexInclusive = Math.Max(0, Math.Min(characterToMeasureEndIndexInclusive, text.Length - 1));
                for (int i = characterToMeasureStartIndexInclusive; i <= characterToMeasureEndIndexInclusive; i++)
                {
                    CheckForBetterClickPosition(ref position, ref clostestIndex, ref clostestXDistSquared, ref clostestYDistSquared, ref offset, i);

                    if(text[i] == '\r')
                    {
                        throw new Exception("All \\r's should have been converted to \\n's.");
                    }

                    if (text[i] == '\n')
                    {
                        offset.x = 0;
                        offset.y -= typeFaceStyle.EmSizeInPixels;
                    }
                    else
                    {
                        Vector2 nextSize;
                        GetOffset(i, i, out nextSize);

                        offset.x += nextSize.x;
                    }
                }

                CheckForBetterClickPosition(ref position, ref clostestIndex, ref clostestXDistSquared, ref clostestYDistSquared, ref offset, characterToMeasureEndIndexInclusive + 1);
            }

            return clostestIndex;
        }

        private static void CheckForBetterClickPosition(ref Vector2 position, ref int clostestIndex, ref double clostestXDistSquared, ref double clostestYDistSquared, ref Vector2 offset, int i)
        {
            Vector2 delta = position - offset;
            double deltaYLengthSquared = delta.y * delta.y;
            if (deltaYLengthSquared < clostestYDistSquared)
            {
                clostestYDistSquared = deltaYLengthSquared;
                clostestXDistSquared = delta.x * delta.x;
                clostestIndex = i;
            }
            else if (deltaYLengthSquared == clostestYDistSquared)
            {
                double deltaXLengthSquared = delta.x * delta.x;
                if (deltaXLengthSquared < clostestXDistSquared)
                {
                    clostestXDistSquared = deltaXLengthSquared;
                    clostestIndex = i;
                }
            }
        }
    }
}

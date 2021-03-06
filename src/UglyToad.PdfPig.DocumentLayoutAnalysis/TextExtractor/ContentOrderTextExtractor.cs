﻿namespace UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor
{
    using System;
    using System.Text;
    using Content;
    using Util;

    /// <summary>
    /// Extracts text from a document based on the content order in the file.
    /// </summary>
    public static class ContentOrderTextExtractor
    {
        /// <summary>
        /// Gets a human readable representation of the text from the page based on
        /// the letter order of the original PDF document.
        /// </summary>
        /// <param name="page">A page from the document.</param>
        /// <param name="addDoubleNewline">Whether to include a double new-line when the text is likely to be a new paragraph.</param>
        public static string GetText(Page page, bool addDoubleNewline = false)
        {
            var sb = new StringBuilder();

            var previous = default(Letter);
            var hasJustAddedWhitespace = false;
            for (var i = 0; i < page.Letters.Count; i++)
            {
                var letter = page.Letters[i];

                if (string.IsNullOrEmpty(letter.Value))
                {
                    continue;
                }

                if (letter.Value == " " && !hasJustAddedWhitespace)
                {
                    if (previous != null && IsNewline(previous, letter, page, out _))
                    {
                        continue;
                    }

                    sb.Append(" ");
                    previous = letter;
                    hasJustAddedWhitespace = true;
                    continue;
                }

                hasJustAddedWhitespace = false;

                if (previous != null && letter.Value != " ")
                {
                    var nwPrevious = GetNonWhitespacePrevious(page, i);

                    if (IsNewline(nwPrevious, letter, page, out var isDoubleNewline))
                    {
                        if (previous.Value == " ")
                        {
                            sb.Remove(sb.Length - 1, 1);
                        }

                        sb.AppendLine();
                        if (addDoubleNewline && isDoubleNewline)
                        {
                            sb.AppendLine();
                        }

                        hasJustAddedWhitespace = true;
                    }
                    else if (previous.Value != " ")
                    {
                        var gap = letter.StartBaseLine.X - previous.EndBaseLine.X;
                        
                        if (WhitespaceSizeStatistics.IsProbablyWhitespace(gap, previous))
                        {
                            sb.Append(" ");
                            hasJustAddedWhitespace = true;
                        }
                    }
                }

                sb.Append(letter.Value);
                previous = letter;
            }

            return sb.ToString();
        }

        private static Letter GetNonWhitespacePrevious(Page page, int index)
        {
            for (var i = index - 1; i >= 0; i--)
            {
                var letter = page.Letters[i];
                if (!string.IsNullOrWhiteSpace(letter.Value))
                {
                    return letter;
                }
            }

            return null;
        }

        private static bool IsNewline(Letter previous, Letter letter, Page page, out bool isDoubleNewline)
        {
            isDoubleNewline = false;

            if (previous == null)
            {
                return false;
            }

            var ptSizePrevious = (int)Math.Round(page.ExperimentalAccess.GetPointSize(previous));
            var ptSize = (int)Math.Round(page.ExperimentalAccess.GetPointSize(letter));
            var minPtSize = ptSize < ptSizePrevious ? ptSize : ptSizePrevious;

            var gap = Math.Abs(previous.StartBaseLine.Y - letter.StartBaseLine.Y);

            if (gap > minPtSize * 1.7 && previous.StartBaseLine.Y > letter.StartBaseLine.Y)
            {
                isDoubleNewline = true;
            }

            return gap > minPtSize * 0.9;
        }
    }
}

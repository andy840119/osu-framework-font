// Copyright (c) karaoke.dev <contact@karaoke.dev>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Layout;
using osu.Framework.Text;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Graphics.Sprites;

public partial class LyricSpriteText
{
    #region Text builder

    /// <summary>
    /// The characters that should be excluded from fixed-width application. Defaults to (".", ",", ":", " ") if null.
    /// </summary>
    protected virtual char[]? FixedWidthExcludeCharacters => null;

    /// <summary>
    /// The character to use to calculate the fixed width width. Defaults to 'm'.
    /// </summary>
    protected virtual char FixedWidthReferenceCharacter => 'm';

    /// <summary>
    /// The character to fallback to use if a character glyph lookup failed.
    /// </summary>
    protected virtual char FallbackCharacter => '?';

    private readonly LayoutValue<TextBuilder> textBuilderCache = new(Invalidation.DrawSize, InvalidationSource.Parent);
    private readonly LayoutValue<TextBuilder> rubyTextBuilderCache = new(Invalidation.DrawSize, InvalidationSource.Parent);
    private readonly LayoutValue<TextBuilder> romajiTextBuilderCache = new(Invalidation.DrawSize, InvalidationSource.Parent);

    /// <summary>
    /// Invalidates the current <see cref="TextBuilder"/>, causing a new one to be created next time it's required via <see cref="CreateTextBuilder"/>.
    /// </summary>
    protected void InvalidateTextBuilder()
    {
        textBuilderCache.Invalidate();
        rubyTextBuilderCache.Invalidate();
        romajiTextBuilderCache.Invalidate();
    }

    /// <summary>
    /// Creates a <see cref="TextBuilder"/> to generate the character layout for this <see cref="LyricSpriteText"/>.
    /// </summary>
    /// <param name="store">The <see cref="ITexturedGlyphLookupStore"/> where characters should be retrieved from.</param>
    /// <returns>The <see cref="TextBuilder"/>.</returns>
    protected virtual TextBuilder CreateTextBuilder(ITexturedGlyphLookupStore store)
    {
        var excludeCharacters = FixedWidthExcludeCharacters ?? default_never_fixed_width_characters;

        var rubyHeight = ReserveRubyHeight || Rubies.Any() ? RubyFont.Size : 0;
        var romajiHeight = ReserveRomajiHeight || Romajies.Any() ? RomajiFont.Size : 0;
        var startOffset = new Vector2(Padding.Left, Padding.Top + rubyHeight);
        var mainTextSpacing = Spacing + new Vector2(0, rubyHeight + romajiHeight);

        float builderMaxWidth = requiresAutoSizedWidth
            ? MaxWidth
            : ApplyRelativeAxes(RelativeSizeAxes, new Vector2(Math.Min(MaxWidth, base.Width), base.Height), FillMode).X - Padding.Right;

        if (AllowMultiline)
        {
            return new MultilineTextBuilder(store, Font, builderMaxWidth, UseFullGlyphHeight, startOffset, mainTextSpacing, null,
                excludeCharacters, FallbackCharacter, FixedWidthReferenceCharacter);
        }

        if (Truncate)
        {
            return new TruncatingTextBuilder(store, Font, builderMaxWidth, ellipsisString, UseFullGlyphHeight, startOffset, mainTextSpacing, null,
                excludeCharacters, FallbackCharacter, FixedWidthReferenceCharacter);
        }

        return new TextBuilder(store, Font, builderMaxWidth, UseFullGlyphHeight, startOffset, mainTextSpacing, null,
            excludeCharacters, FallbackCharacter, FixedWidthReferenceCharacter);
    }

    protected virtual TextBuilder CreateRubyTextBuilder(ITexturedGlyphLookupStore store)
    {
        const int builder_max_width = int.MaxValue;
        var excludeCharacters = FixedWidthExcludeCharacters ?? default_never_fixed_width_characters;

        return new TextBuilder(store, RubyFont, builder_max_width, UseFullGlyphHeight,
            new Vector2(), rubySpacing, null, excludeCharacters, FallbackCharacter, FixedWidthReferenceCharacter);
    }

    protected virtual TextBuilder CreateRomajiTextBuilder(ITexturedGlyphLookupStore store)
    {
        const int builder_max_width = int.MaxValue;
        var excludeCharacters = FixedWidthExcludeCharacters ?? default_never_fixed_width_characters;

        return new TextBuilder(store, RomajiFont, builder_max_width, UseFullGlyphHeight,
            new Vector2(), romajiSpacing, null, excludeCharacters, FallbackCharacter, FixedWidthReferenceCharacter);
    }

    private TextBuilder getTextBuilder()
    {
        if (!textBuilderCache.IsValid)
            textBuilderCache.Value = CreateTextBuilder(store);

        return textBuilderCache.Value;
    }

    private TextBuilder getRubyTextBuilder()
    {
        if (!rubyTextBuilderCache.IsValid)
            rubyTextBuilderCache.Value = CreateRubyTextBuilder(store);

        return rubyTextBuilderCache.Value;
    }

    private TextBuilder getRomajiTextBuilder()
    {
        if (!romajiTextBuilderCache.IsValid)
            romajiTextBuilderCache.Value = CreateRomajiTextBuilder(store);

        return romajiTextBuilderCache.Value;
    }

    public float LineBaseHeight
    {
        get
        {
            computeCharacters();
            return textBuilderCache.Value.LineBaseHeight;
        }
    }

    #endregion

    #region Characters

    private readonly LayoutValue charactersCache = new LayoutValue(Invalidation.DrawSize | Invalidation.Presence, InvalidationSource.Parent);

    /// <summary>
    /// Glyph list to be passed to <see cref="TextBuilder"/>.
    /// </summary>
    private readonly List<TextBuilderGlyph> charactersBacking = new List<TextBuilderGlyph>();

    /// <summary>
    /// The characters in local space.
    /// </summary>
    private IReadOnlyList<TextBuilderGlyph> characters
    {
        get
        {
            computeCharacters();
            return charactersBacking;
        }
    }

    /// <summary>
    /// Glyph list to be passed to <see cref="TextBuilder"/>.
    /// </summary>
    private readonly Dictionary<PositionText, PositionTextBuilderGlyph[]> rubyCharactersBacking = new Dictionary<PositionText, PositionTextBuilderGlyph[]>();

    /// <summary>
    /// The characters in local space.
    /// </summary>
    private IReadOnlyDictionary<PositionText, PositionTextBuilderGlyph[]> rubyCharacters
    {
        get
        {
            computeCharacters();
            return rubyCharactersBacking;
        }
    }

    /// <summary>
    /// Glyph list to be passed to <see cref="TextBuilder"/>.
    /// </summary>
    private readonly Dictionary<PositionText, PositionTextBuilderGlyph[]> romajiCharactersBacking = new Dictionary<PositionText, PositionTextBuilderGlyph[]>();

    /// <summary>
    /// The characters in local space.
    /// </summary>
    private IReadOnlyDictionary<PositionText, PositionTextBuilderGlyph[]> romajiCharacters
    {
        get
        {
            computeCharacters();
            return romajiCharactersBacking;
        }
    }

    /// <summary>
    /// Compute character textures and positions.
    /// </summary>
    private void computeCharacters()
    {
        // Note : this feature can only use in osu-framework
        // if (LoadState >= LoadState.Loaded)
        //     ThreadSafety.EnsureUpdateThread();

        if (store == null)
            return;

        if (charactersCache.IsValid)
            return;

        charactersBacking.Clear();
        rubyCharactersBacking.Clear();
        romajiCharactersBacking.Clear();

        // Todo: Re-enable this assert after autosize is split into two passes.
        // Debug.Assert(!isComputingCharacters, "Cyclic invocation of computeCharacters()!");

        Vector2 textBounds = Vector2.Zero;

        try
        {
            if (string.IsNullOrEmpty(displayedText))
                return;

            // Main text
            var textBuilder = getTextBuilder();
            charactersBacking.AddRange(applyTextToBuilder(textBuilder, displayedText));

            // Ruby
            var rubyTextBuilder = getRubyTextBuilder();
            var rubyTextFormatter = new PositionTextFormatter(charactersBacking, RelativePosition.Top, rubyAlignment, rubySpacing, rubyMargin);

            foreach (var (positionText, textBuilderGlyphs) in applyPositionTextToBuilder(rubyTextBuilder, displayedText, rubies))
            {
                rubyCharactersBacking.Add(positionText, rubyTextFormatter.Calculate(positionText, textBuilderGlyphs));
            }

            // Romaji
            var romajiTextBuilder = getRomajiTextBuilder();
            var romajiTextFormatter = new PositionTextFormatter(charactersBacking, RelativePosition.Bottom, romajiAlignment, romajiSpacing, romajiMargin);

            foreach (var (positionText, textBuilderGlyphs) in applyPositionTextToBuilder(romajiTextBuilder, displayedText, romajies))
            {
                romajiCharactersBacking.Add(positionText, romajiTextFormatter.Calculate(positionText, textBuilderGlyphs));
            }

            textBounds = textBuilder.Bounds;
        }
        finally
        {
            if (requiresAutoSizedWidth)
                base.Width = textBounds.X + Padding.Right;

            if (requiresAutoSizedHeight)
            {
                var romajiHeight = ReserveRomajiHeight || Romajies.Any() ? RomajiFont.Size : 0;
                base.Height = textBounds.Y + romajiHeight + Padding.Bottom;
            }

            base.Width = Math.Min(base.Width, MaxWidth);

            charactersCache.Validate();
        }
    }

    private static IEnumerable<TextBuilderGlyph> applyTextToBuilder(TextBuilder textBuilder, string text)
    {
        textBuilder.Reset();
        textBuilder.AddText(text);

        return textBuilder.Characters;
    }

    private static Dictionary<PositionText, TextBuilderGlyph[]> applyPositionTextToBuilder(TextBuilder textBuilder, string text, IEnumerable<PositionText> positionTexts)
    {
        var fixedPositionTexts = GetFixedPositionTexts(positionTexts, text);

        var texts = new Dictionary<PositionText, TextBuilderGlyph[]>();

        foreach (var positionText in fixedPositionTexts)
        {
            textBuilder.Reset();
            textBuilder.AddText(positionText.Text);

            texts.Add(positionText, textBuilder.Characters.ToArray());
        }

        return texts;
    }

    internal static List<PositionText> GetFixedPositionTexts(IEnumerable<PositionText> positionTexts, string lyricText)
        => positionTexts
           .Select(x => GetFixedPositionText(x, lyricText))
           .OfType<PositionText>()
           .Distinct()
           .ToList();

    internal static PositionText? GetFixedPositionText(PositionText positionText, string lyricText)
    {
        if (string.IsNullOrEmpty(lyricText))
            return null;

        var startIndex = Math.Clamp(positionText.StartIndex, 0, lyricText.Length - 1);
        var endIndex = Math.Clamp(positionText.EndIndex, 0, lyricText.Length - 1);
        var text = string.IsNullOrEmpty(positionText.Text) ? " " : positionText.Text;
        return new PositionText(text, Math.Min(startIndex, endIndex), Math.Max(startIndex, endIndex));
    }

    #endregion

    #region Screen space characters

    private readonly LayoutValue parentScreenSpaceCache = new LayoutValue(Invalidation.DrawSize | Invalidation.Presence | Invalidation.DrawInfo, InvalidationSource.Parent);
    private readonly LayoutValue localScreenSpaceCache = new LayoutValue(Invalidation.MiscGeometry, InvalidationSource.Self);

    private readonly List<ScreenSpaceCharacterPart> screenSpaceCharactersBacking = new List<ScreenSpaceCharacterPart>();

    /// <summary>
    /// The characters in screen space. These are ready to be drawn.
    /// </summary>
    private IEnumerable<ScreenSpaceCharacterPart> screenSpaceCharacters
    {
        get
        {
            computeScreenSpaceCharacters();
            return screenSpaceCharactersBacking;
        }
    }

    private void computeScreenSpaceCharacters()
    {
        if (!parentScreenSpaceCache.IsValid)
        {
            localScreenSpaceCache.Invalidate();
            parentScreenSpaceCache.Validate();
        }

        if (localScreenSpaceCache.IsValid)
            return;

        screenSpaceCharactersBacking.Clear();

        Vector2 inflationAmount = DrawInfo.MatrixInverse.ExtractScale().Xy;

        foreach (var character in characters)
        {
            screenSpaceCharactersBacking.Add(new ScreenSpaceCharacterPart
            {
                DrawQuad = ToScreenSpace(character.DrawRectangle.Inflate(inflationAmount)),
                InflationPercentage = Vector2.Divide(inflationAmount, character.DrawRectangle.Size),
                Texture = character.Texture
            });
        }

        var positionCharacters = new List<PositionTextBuilderGlyph>()
                                 .Concat(rubyCharacters.SelectMany(x => x.Value))
                                 .Concat(romajiCharacters.SelectMany(x => x.Value));

        foreach (var character in positionCharacters)
        {
            screenSpaceCharactersBacking.Add(new ScreenSpaceCharacterPart
            {
                DrawQuad = ToScreenSpace(character.DrawRectangle.Inflate(inflationAmount)),
                InflationPercentage = Vector2.Divide(inflationAmount, character.DrawRectangle.Size),
                Texture = character.Texture
            });
        }

        localScreenSpaceCache.Validate();
    }

    #endregion

    #region Character position

    public float GetTextIndexXPosition(TextIndex index)
    {
        var computedRectangle = GetCharacterDrawRectangle(index.Index);
        return index.State == TextIndex.IndexState.Start ? computedRectangle.Left : computedRectangle.Right;
    }

    public RectangleF GetCharacterDrawRectangle(int index, bool drawSizeOnly = false)
    {
        int charIndex = Math.Clamp(index, 0, Text.Length - 1);
        if (charIndex != index)
            throw new ArgumentOutOfRangeException(nameof(index));

        var character = characters[charIndex];
        var drawRectangle = drawSizeOnly ? character.DrawRectangle : TextBuilderGlyphUtils.GetCharacterSizeRectangle(character);
        return getComputeCharacterDrawRectangle(drawRectangle);
    }

    public RectangleF? GetRubyTagDrawRectangle(PositionText rubyTag, bool drawSizeOnly = false)
    {
        var fixedRubyTag = GetFixedPositionText(rubyTag, displayedText);
        if (fixedRubyTag == null)
            return null;

        if (!rubyCharacters.TryGetValue(fixedRubyTag.Value, out var glyphs))
            throw new ArgumentOutOfRangeException(nameof(fixedRubyTag));

        var drawRectangle = glyphs.Select(x => drawSizeOnly ? x.DrawRectangle : TextBuilderGlyphUtils.GetCharacterSizeRectangle(x))
                                  .Aggregate(RectangleF.Union);
        return getComputeCharacterDrawRectangle(drawRectangle);
    }

    public RectangleF? GetRomajiTagDrawRectangle(PositionText romajiTag, bool drawSizeOnly = false)
    {
        var fixedRomajiTag = GetFixedPositionText(romajiTag, displayedText);
        if (fixedRomajiTag == null)
            return null;

        if (!romajiCharacters.TryGetValue(fixedRomajiTag.Value, out var glyphs))
            throw new ArgumentOutOfRangeException(nameof(fixedRomajiTag));

        var drawRectangle = glyphs.Select(x => drawSizeOnly ? x.DrawRectangle : TextBuilderGlyphUtils.GetCharacterSizeRectangle(x))
                                  .Aggregate(RectangleF.Union);
        return getComputeCharacterDrawRectangle(drawRectangle);
    }

    private int skinIndex(IEnumerable<PositionText> positionTexts, int endIndex)
        => positionTexts.Where((_, i) => i < endIndex).Sum(x => x.Text.Length);

    private RectangleF getComputeCharacterDrawRectangle(RectangleF originalCharacterDrawRectangle)
    {
        // combine the rectangle to get the max value.
        return Shaders.OfType<IApplicableToCharacterSize>()
                      .Select(x => x.ComputeCharacterDrawRectangle(originalCharacterDrawRectangle))
                      .Aggregate(originalCharacterDrawRectangle, RectangleF.Union);
    }

    #endregion
}

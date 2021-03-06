// Copyright (c) andy840119 <andy840119@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.IO.Stores;
using osu.Framework.Utils;
using osu.Framework.Text;
using osuTK;
using osuTK.Graphics;
using System.Linq;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A container for simple text rendering purposes. If more complex text rendering is required, use <see cref="TextFlowContainer"/> instead.
    /// </summary>
    public partial class LyricSpriteText : Drawable, IHasLineBaseHeight, ITexturedShaderDrawable, IHasFilterTerms, IFillFlowContainer, IHasCurrentValue<string>, IHasRuby, IHasRomaji, IHasTexture
    {
        private const float default_text_size = 48;
        private static readonly char[] default_never_fixed_width_characters = { '.', ',', ':', ' ' };

        [Resolved]
        private FontStore store { get; set; }

        public IShader TextureShader { get; private set; }
        public IShader RoundedTextureShader { get; private set; }

        public LyricSpriteText()
        {
            Font = new FontUsage(null, default_text_size);
            current.BindValueChanged(text => Text = text.NewValue);

            AddLayout(charactersCache);
            AddLayout(parentScreenSpaceCache);
            AddLayout(localScreenSpaceCache);
            AddLayout(shadowOffsetCache);
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);

            // Pre-cache the characters in the texture store
            foreach (var character in displayedText)
            {
                var unused = store.Get(font.FontName, character) ?? store.Get(null, character);
            }
        }

        #region text

        private string text = string.Empty;

        /// <summary>
        /// Gets or sets the text to be displayed.
        /// </summary>
        public string Text
        {
            get => text;
            set
            {
                if (text == value)
                    return;

                text = value;

                current.Value = text;

                invalidate(true);
            }
        }

        private readonly BindableWithCurrent<string> current = new BindableWithCurrent<string>();

        public Bindable<string> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private string displayedText => Text;

        private PositionText[] rubies;

        /// <summary>
        /// Gets or sets the ruby text to be displayed.
        /// </summary>
        public PositionText[] Rubies
        {
            get => rubies;
            set
            {
                rubies = filterValidValues(value);

                invalidate(true);
            }
        }

        private PositionText[] romajies;

        /// <summary>
        /// Gets or sets the romaji text to be displayed.
        /// </summary>
        public PositionText[] Romajies
        {
            get => romajies;
            set
            {
                romajies = filterValidValues(value);

                invalidate(true);
            }
        }

        private PositionText[] filterValidValues(PositionText[] texts)
        {
            return texts?.Where(positionText => Math.Min(positionText.StartIndex, positionText.EndIndex) >= 0
                                                && Math.Max(positionText.StartIndex, positionText.EndIndex) <= text.Length
                                                && positionText.EndIndex > positionText.StartIndex).ToArray();
        }

        #endregion

        #region font

        private FontUsage font = FontUsage.Default;

        /// <summary>
        /// Contains information on the font used to display the text.
        /// </summary>
        public FontUsage Font
        {
            get => font;
            set
            {
                font = value;

                invalidate(true);
                shadowOffsetCache.Invalidate();
            }
        }

        private FontUsage rubyFont = FontUsage.Default;

        /// <summary>
        /// Contains information on the font used to display the text.
        /// </summary>
        public FontUsage RubyFont
        {
            get => rubyFont;
            set
            {
                rubyFont = value;

                invalidate(true);
                shadowOffsetCache.Invalidate();
            }
        }

        private FontUsage romajiFont = FontUsage.Default;

        /// <summary>
        /// Contains information on the font used to display the text.
        /// </summary>
        public FontUsage RomajiFont
        {
            get => romajiFont;
            set
            {
                romajiFont = value;

                invalidate(true);
                shadowOffsetCache.Invalidate();
            }
        }

        #endregion

        #region style

        private bool allowMultiline = true;

        /// <summary>
        /// True if the text should be wrapped if it gets too wide. Note that \n does NOT cause a line break. If you need explicit line breaks, use <see cref="TextFlowContainer"/> instead.
        /// </summary>
        /// <remarks>
        /// If enabled, <see cref="Truncate"/> will be disabled.
        /// </remarks>
        public bool AllowMultiline
        {
            get => allowMultiline;
            set
            {
                if (allowMultiline == value)
                    return;

                if (value)
                    Truncate = false;

                allowMultiline = value;

                invalidate(true);
            }
        }

        private bool shadow;

        /// <summary>
        /// True if a shadow should be displayed around the text.
        /// </summary>
        public bool Shadow
        {
            get => shadow;
            set
            {
                if (shadow == value)
                    return;

                shadow = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        private Color4 shadowColour = new Color4(0, 0, 0, 0.2f);

        /// <summary>
        /// The colour of the shadow displayed around the text. A shadow will only be displayed if the <see cref="Shadow"/> property is set to true.
        /// </summary>
        public Color4 ShadowColour
        {
            get => shadowColour;
            set
            {
                if (shadowColour == value)
                    return;

                shadowColour = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        private Vector2 shadowOffset = new Vector2(0, 0.06f);

        /// <summary>
        /// The offset of the shadow displayed around the text. A shadow will only be displayed if the <see cref="Shadow"/> property is set to true.
        /// </summary>
        public Vector2 ShadowOffset
        {
            get => shadowOffset;
            set
            {
                // Need to change size to percentage of font size.
                var percentagedSize = value / Font.Size;
                if (shadowOffset == percentagedSize)
                    return;

                shadowOffset = percentagedSize;

                invalidate(true);
                shadowOffsetCache.Invalidate();
            }
        }

        private bool useFullGlyphHeight = true;

        /// <summary>
        /// True if the <see cref="LyricSpriteText"/>'s vertical size should be equal to <see cref="FontUsage.Size"/>  (the full height) or precisely the size of used characters.
        /// Set to false to allow better centering of individual characters/numerals/etc.
        /// </summary>
        public bool UseFullGlyphHeight
        {
            get => useFullGlyphHeight;
            set
            {
                if (useFullGlyphHeight == value)
                    return;

                useFullGlyphHeight = value;

                invalidate(true);
            }
        }

        private ILyricTexture textTexture;

        public ILyricTexture TextTexture
        {
            get => textTexture;
            set
            {
                if (textTexture == value)
                    return;

                textTexture = value;
                Colour = (textTexture as SolidTexture)?.SolidColor ?? Color4.White;

                Invalidate(Invalidation.All);
            }
        }

        private ILyricTexture shadowTexture;

        public ILyricTexture ShadowTexture
        {
            get => shadowTexture;
            set
            {
                if (shadowTexture == value)
                    return;

                shadowTexture = value;
                ShadowColour = (shadowTexture as SolidTexture)?.SolidColor ?? Color4.White;
                Invalidate(Invalidation.All);
            }
        }

        private ILyricTexture borderTexture;

        public ILyricTexture BorderTexture
        {
            get => borderTexture;
            set
            {
                if (borderTexture == value)
                    return;

                borderTexture = value;
                Invalidate(Invalidation.All);
            }
        }

        private LyricTextAlignment rubyAlignment;

        /// <summary>
        /// Gets or sets the ruby alignment.
        /// </summary>
        public LyricTextAlignment RubyAlignment
        {
            get => rubyAlignment;
            set
            {
                if (rubyAlignment == value)
                    return;

                rubyAlignment = value;
                invalidate(true);
            }
        }

        private LyricTextAlignment romajiAlignment;

        /// <summary>
        /// Gets or sets the romaji alignment.
        /// </summary>
        public LyricTextAlignment RomajiAlignment
        {
            get => romajiAlignment;
            set
            {
                if (romajiAlignment == value)
                    return;

                romajiAlignment = value;
                invalidate(true);
            }
        }

        private float borderRadius;

        /// <summary>
        /// Gets or sets the border redius
        /// </summary>
        public float BorderRadius
        {
            get => borderRadius;
            set
            {
                if (borderRadius == value)
                    return;

                borderRadius = value;

                invalidate(true);
            }
        }

        private bool border;

        /// <summary>
        /// Gets or sets the border
        /// </summary>
        public bool Border
        {
            get => border;
            set
            {
                if (border == value)
                    return;

                border = value;

                invalidate(true);
            }
        }

        private bool truncate;

        /// <summary>
        /// If true, text should be truncated when it exceeds the <see cref="Drawable.DrawWidth"/> of this <see cref="LyricSpriteText"/>.
        /// </summary>
        /// <remarks>
        /// Has no effect if no <see cref="Width"/> or custom sizing is set.
        /// If enabled, <see cref="AllowMultiline"/> will be disabled.
        /// </remarks>
        public bool Truncate
        {
            get => truncate;
            set
            {
                if (truncate == value) return;

                if (value)
                    AllowMultiline = false;

                truncate = value;
                invalidate(true);
            }
        }

        private string ellipsisString = "…";

        /// <summary>
        /// When <see cref="Truncate"/> is enabled, this decides what string is used to signify that truncation has occured.
        /// Defaults to "…".
        /// </summary>
        public string EllipsisString
        {
            get => ellipsisString;
            set
            {
                if (ellipsisString == value) return;

                ellipsisString = value;
                invalidate(true);
            }
        }

        #endregion

        #region size

        private bool requiresAutoSizedWidth => explicitWidth == null && (RelativeSizeAxes & Axes.X) == 0;

        private bool requiresAutoSizedHeight => explicitHeight == null && (RelativeSizeAxes & Axes.Y) == 0;

        private float? explicitWidth;

        /// <summary>
        /// Gets or sets the width of this <see cref="LyricSpriteText"/>. The <see cref="LyricSpriteText"/> will maintain this width when set.
        /// </summary>
        public override float Width
        {
            get
            {
                if (requiresAutoSizedWidth)
                    computeCharacters();
                return base.Width;
            }
            set
            {
                if (explicitWidth == value)
                    return;

                base.Width = value;
                explicitWidth = value;

                invalidate(true);
            }
        }

        private float maxWidth = float.PositiveInfinity;

        /// <summary>
        /// The maximum width of this <see cref="LyricSpriteText"/>. Affects both auto and fixed sizing modes.
        /// </summary>
        /// <remarks>
        /// This becomes a relative value if this <see cref="LyricSpriteText"/> is relatively-sized on the X-axis.
        /// </remarks>
        public float MaxWidth
        {
            get => maxWidth;
            set
            {
                if (maxWidth == value)
                    return;

                maxWidth = value;
                invalidate(true);
            }
        }

        private float? explicitHeight;

        /// <summary>
        /// Gets or sets the height of this <see cref="LyricSpriteText"/>. The <see cref="LyricSpriteText"/> will maintain this height when set.
        /// </summary>
        public override float Height
        {
            get
            {
                if (requiresAutoSizedHeight)
                    computeCharacters();
                return base.Height;
            }
            set
            {
                if (explicitHeight == value)
                    return;

                base.Height = value;
                explicitHeight = value;

                invalidate(true);
            }
        }

        /// <summary>
        /// Gets or sets the size of this <see cref="LyricSpriteText"/>. The <see cref="LyricSpriteText"/> will maintain this size when set.
        /// </summary>
        public override Vector2 Size
        {
            get
            {
                if (requiresAutoSizedWidth || requiresAutoSizedHeight)
                    computeCharacters();
                return base.Size;
            }
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }

        #endregion

        #region text spacing

        private Vector2 spacing;

        /// <summary>
        /// Gets or sets the spacing between characters of this <see cref="LyricSpriteText"/>.
        /// </summary>
        public Vector2 Spacing
        {
            get => spacing;
            set
            {
                if (spacing == value)
                    return;

                spacing = value;

                invalidate(true);
            }
        }

        private Vector2 rubySpacing;

        /// <summary>
        /// Gets or sets the spacing between characters of ruby text.
        /// </summary>
        public Vector2 RubySpacing
        {
            get => rubySpacing;
            set
            {
                if (rubySpacing == value)
                    return;

                rubySpacing = value;

                invalidate(true);
            }
        }

        private Vector2 romajiSpacing;

        /// <summary>
        /// Gets or sets the spacing between characters of romaji text.
        /// </summary>
        public Vector2 RomajiSpacing
        {
            get => romajiSpacing;
            set
            {
                if (romajiSpacing == value)
                    return;

                romajiSpacing = value;

                invalidate(true);
            }
        }

        #endregion

        #region margin/padding

        private MarginPadding padding;

        /// <summary>
        /// Shrinks the space which may be occupied by characters of this <see cref="LyricSpriteText"/> by the specified amount on each side.
        /// </summary>
        public MarginPadding Padding
        {
            get => padding;
            set
            {
                if (padding.Equals(value))
                    return;

                if (!Validation.IsFinite(value)) throw new ArgumentException($@"{nameof(Padding)} must be finite, but is {value}.");

                padding = value;

                invalidate(true);
            }
        }

        private int rubyMargin;

        /// <summary>
        /// Shrinks the space between ruby and main text.
        /// </summary>
        public int RubyMargin
        {
            get => rubyMargin;
            set
            {
                if (rubyMargin == value)
                    return;

                rubyMargin = value;

                invalidate(true);
            }
        }

        private int romajiMargin;

        /// <summary>
        /// Shrinks the space between romaji and main text.
        /// </summary>
        public int RomajiMargin
        {
            get => romajiMargin;
            set
            {
                if (romajiMargin == value)
                    return;

                romajiMargin = value;

                invalidate(true);
            }
        }

        private bool reserveRubyHeight;

        /// <summary>
        /// Reserve ruby height even contains no ruby.
        /// </summary>
        public bool ReserveRubyHeight
        {
            get => reserveRubyHeight;
            set
            {
                if (reserveRubyHeight == value)
                    return;

                reserveRubyHeight = value;

                invalidate(true);
            }
        }

        private bool reserveRomajiHeight;

        /// <summary>
        /// Reserve romaji height even contains no ruby.
        /// </summary>
        public bool ReserveRomajiHeight
        {
            get => reserveRomajiHeight;
            set
            {
                if (reserveRomajiHeight == value)
                    return;

                reserveRomajiHeight = value;

                invalidate(true);
            }
        }

        #endregion

        public override bool IsPresent => base.IsPresent && (AlwaysPresent || !string.IsNullOrEmpty(displayedText));

        #region Invalidation

        private void invalidate(bool layout = false)
        {
            if (layout)
                charactersCache.Invalidate();
            parentScreenSpaceCache.Invalidate();
            localScreenSpaceCache.Invalidate();

            Invalidate(Invalidation.DrawNode);
        }

        #endregion

        /// <summary>
        /// The characters that should be excluded from fixed-width application. Defaults to (".", ",", ":", " ") if null.
        /// </summary>
        protected virtual char[] FixedWidthExcludeCharacters => null;

        /// <summary>
        /// The character to use to calculate the fixed width width. Defaults to 'm'.
        /// </summary>
        protected virtual char FixedWidthReferenceCharacter => 'm';

        /// <summary>
        /// The character to fallback to use if a character glyph lookup failed.
        /// </summary>
        protected virtual char FallbackCharacter => '?';

        /// <summary>
        /// Creates a <see cref="TextBuilder"/> to generate the character layout for this <see cref="LyricSpriteText"/>.
        /// </summary>
        /// <param name="store">The <see cref="ITexturedGlyphLookupStore"/> where characters should be retrieved from.</param>
        /// <returns>The <see cref="TextBuilder"/>.</returns>
        protected virtual TextBuilder CreateTextBuilder(ITexturedGlyphLookupStore store)
        {
            var excludeCharacters = FixedWidthExcludeCharacters ?? default_never_fixed_width_characters;

            var rubyHeight = (ReserveRubyHeight || (Rubies?.Any() ?? false)) ? RubyFont.Size : 0;
            var romajiHeight = (ReserveRomajiHeight || (Romajies?.Any() ?? false)) ? RomajiFont.Size : 0;
            var startOffset = new Vector2(Padding.Left, Padding.Top + rubyHeight);
            var spacing = Spacing + new Vector2(0, rubyHeight + romajiHeight);

            float builderMaxWidth = requiresAutoSizedWidth
                ? MaxWidth
                : ApplyRelativeAxes(RelativeSizeAxes, new Vector2(Math.Min(MaxWidth, base.Width), base.Height), FillMode).X - Padding.Right;

            if (AllowMultiline)
            {
                return new MultilineTextBuilder(store, Font, builderMaxWidth, UseFullGlyphHeight, startOffset, spacing, charactersBacking,
                    excludeCharacters, FallbackCharacter, FixedWidthReferenceCharacter);
            }

            if (Truncate)
            {
                return new TruncatingTextBuilder(store, Font, builderMaxWidth, ellipsisString, UseFullGlyphHeight, startOffset, spacing, charactersBacking,
                    excludeCharacters, FallbackCharacter, FixedWidthReferenceCharacter);
            }

            return new TextBuilder(store, Font, builderMaxWidth, UseFullGlyphHeight, startOffset, spacing, charactersBacking,
                excludeCharacters, FallbackCharacter, FixedWidthReferenceCharacter);
        }

        protected virtual PositionTextBuilder CreateRubyTextBuilder(ITexturedGlyphLookupStore store)
        {
            const int builder_max_width = int.MaxValue;
            return new PositionTextBuilder(store, Font, RubyFont, builder_max_width, UseFullGlyphHeight,
                new Vector2(0, -rubyMargin), rubySpacing, charactersBacking, FixedWidthExcludeCharacters, FallbackCharacter, FixedWidthReferenceCharacter, RelativePosition.Top, rubyAlignment);
        }

        protected virtual PositionTextBuilder CreateRomajiTextBuilder(ITexturedGlyphLookupStore store)
        {
            const int builder_max_width = int.MaxValue;
            return new PositionTextBuilder(store, Font, RomajiFont, builder_max_width, UseFullGlyphHeight,
                new Vector2(0, romajiMargin), romajiSpacing, charactersBacking, FixedWidthExcludeCharacters, FallbackCharacter, FixedWidthReferenceCharacter, RelativePosition.Bottom, romajiAlignment);
        }

        public override string ToString() => $@"""{displayedText}"" " + base.ToString();

        /// <summary>
        /// Gets the base height of the font used by this text. If the font of this text is invalid, 0 is returned.
        /// </summary>
        public float LineBaseHeight
        {
            get
            {
                var baseHeight = store.GetBaseHeight(Font.FontName);
                if (baseHeight.HasValue)
                    return baseHeight.Value * Font.Size;

                if (string.IsNullOrEmpty(displayedText))
                    return 0;

                return store.GetBaseHeight(displayedText[0]).GetValueOrDefault() * Font.Size;
            }
        }

        public IEnumerable<string> FilterTerms => displayedText.Yield();
    }
}

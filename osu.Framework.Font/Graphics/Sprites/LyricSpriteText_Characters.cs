﻿// Copyright (c) andy840119 <andy840119@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Layout;
using osu.Framework.Text;
using osuTK;

namespace osu.Framework.Graphics.Sprites
{
    public partial class LyricSpriteText
    {
        private readonly LayoutValue charactersCache = new LayoutValue(Invalidation.DrawSize | Invalidation.Presence, InvalidationSource.Parent);

        /// <summary>
        /// Glyph list to be passed to <see cref="TextBuilder"/>.
        /// </summary>
        private readonly List<TextBuilderGlyph> charactersBacking = new List<TextBuilderGlyph>();

        /// <summary>
        /// The characters in local space.
        /// </summary>
        public List<TextBuilderGlyph> Characters
        {
            get
            {
                computeCharacters();
                return charactersBacking;
            }
        }

        private bool isComputingCharacters;

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

            Debug.Assert(!isComputingCharacters, "Cyclic invocation of computeCharacters()!");
            isComputingCharacters = true;

            TextBuilder textBuilder = null;

            try
            {
                if (string.IsNullOrEmpty(displayedText))
                    return;

                textBuilder = CreateTextBuilder(store);
                textBuilder.AddText(displayedText);

                if (rubies?.Any() ?? false)
                {
                    var rubyTextBuilder = CreateRubyTextBuilder(store);
                    rubies.ForEach(x => rubyTextBuilder.AddText(x));
                }

                if (romajies?.Any() ?? false)
                {
                    var romajiTextBuilder = CreateRomajiTextBuilder(store);
                    romajies.ForEach(x => romajiTextBuilder.AddText(x));
                }
            }
            finally
            {
                if (requiresAutoSizedWidth)
                    base.Width = (textBuilder?.Bounds.X ?? 0) + Padding.Right;

                if (requiresAutoSizedHeight)
                {
                    var romajiHeight = (ReserveRomajiHeight || (Romajies?.Any() ?? false)) ? RomajiFont.Size : 0;
                    base.Height = (textBuilder?.Bounds.Y ?? 0) + romajiHeight + Padding.Bottom;
                }

                base.Width = Math.Min(base.Width, MaxWidth);

                isComputingCharacters = false;
                charactersCache.Validate();
            }
        }

        private readonly LayoutValue parentScreenSpaceCache = new LayoutValue(Invalidation.DrawSize | Invalidation.Presence | Invalidation.DrawInfo, InvalidationSource.Parent);
        private readonly LayoutValue localScreenSpaceCache = new LayoutValue(Invalidation.MiscGeometry, InvalidationSource.Self);

        private readonly List<ScreenSpaceCharacterPart> screenSpaceCharactersBacking = new List<ScreenSpaceCharacterPart>();

        /// <summary>
        /// The characters in screen space. These are ready to be drawn.
        /// </summary>
        private List<ScreenSpaceCharacterPart> screenSpaceCharacters
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

            foreach (var character in Characters)
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

        private readonly LayoutValue<Vector2> shadowOffsetCache = new LayoutValue<Vector2>(Invalidation.DrawInfo, InvalidationSource.Parent);

        private Vector2 premultipliedShadowOffset =>
            shadowOffsetCache.IsValid ? shadowOffsetCache.Value : shadowOffsetCache.Value = ToScreenSpace(shadowOffset * Font.Size) - ToScreenSpace(Vector2.Zero);
    }
}

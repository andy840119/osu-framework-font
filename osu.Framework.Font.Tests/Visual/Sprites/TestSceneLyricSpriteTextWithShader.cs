﻿// Copyright (c) andy840119 <andy840119@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Font.Tests.Helper;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Font.Tests.Visual.Sprites
{
    public class TestSceneLyricSpriteTextWithShader : TestScene
    {
        [Resolved]
        private ShaderManager shaderManager { get; set; }

        [TestCase("CRT")]
        [TestCase("not_found_shader")] // notice that missing shader will let whole sprite text being white.
        public void ApplyShader(string shaderName)
        {
            var shader = shaderManager.Load(VertexShaderDescriptor.TEXTURE_2, shaderName);
            AddStep("Create lyric", () => setContents((spriteText) => spriteText.Shaders = new[]
            {
                shader,
            }));
        }

        [TestCase]
        public void ApplyShaderWithParams()
        {
            var shader = shaderManager.Load(VertexShaderDescriptor.TEXTURE_2, "Outline");
            var outlineShader = new OutlineShader(shader)
            {
                Radius = 10,
                OutlineColour = Color4.Green,
            };

            AddStep("Create lyric", () => setContents((spriteText) => spriteText.Shaders = new[]
            {
                outlineShader,
            }));
        }

        private void setContents(Action<LyricSpriteText> applySpriteTextProperty)
        {
            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.CornflowerBlue,
                        RelativeSizeAxes = Axes.Both,
                    },
                    new LyricSpriteText
                    {
                        Text = "カラオケ",
                        Rubies = TestCaseTagHelper.ParseParsePositionTexts(new[] { "[0,1]:か", "[1,2]:ら", "[2,3]:お", "[3,4]:け" }),
                        Romajies = TestCaseTagHelper.ParseParsePositionTexts(new[] { "[0,1]:ka", "[1,2]:ra", "[2,3]:o", "[3,4]:ke" }),
                        Scale = new Vector2(5)
                    }.With(applySpriteTextProperty),
                }
            };
        }
    }
}

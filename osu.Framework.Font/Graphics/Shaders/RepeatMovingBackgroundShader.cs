﻿// Copyright (c) karaoke.dev <contact@karaoke.dev>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Shaders
{
    public class RepeatMovingBackgroundShader : InternalShader, IApplicableToCurrentTime
    {
        public override string ShaderName => "RepeatMovingBackground";

        public Texture Texture { get; set; }

        public Vector2 TextureDisplaySize { get; set; } = new Vector2(10);

        public Vector2 TextureDisplayBorder { get; set; }

        public Vector2 Speed { get; set; }

        public float Mix { get; set; } = 1f;

        public override void ApplyValue(FrameBuffer current)
        {
            if (Texture == null)
                return;

            var size = current.Size;
            GetUniform<Vector2>(@"g_TexSize").UpdateValue(ref size);

            Texture.TextureGL.Bind(TextureUnit.Texture1);

            var unitId = TextureUnit.Texture1 - TextureUnit.Texture0;
            GetUniform<int>(@"g_RepeatSample").UpdateValue(ref unitId);

            var textureCoord = Texture.GetTextureRect().TopLeft;
            GetUniform<Vector2>(@"g_RepeatSampleCoord").UpdateValue(ref textureCoord);

            var textureSize = Texture.GetTextureRect().Size;
            GetUniform<Vector2>(@"g_RepeatSampleSize").UpdateValue(ref textureSize);

            var textureDisplaySize = TextureDisplaySize;
            GetUniform<Vector2>("g_DisplaySize").UpdateValue(ref textureDisplaySize);

            var textureDisplayBorder = TextureDisplayBorder;
            GetUniform<Vector2>("g_DisplayBorder").UpdateValue(ref textureDisplayBorder);

            var speed = Speed;
            GetUniform<Vector2>("g_Speed").UpdateValue(ref speed);

            var mix = Math.Clamp(Mix, 0, 1);
            GetUniform<float>(@"g_Mix").UpdateValue(ref mix);
        }

        public void ApplyCurrentTime(float currentTime)
        {
            GetUniform<float>("g_Time").UpdateValue(ref currentTime);
        }
    }
}
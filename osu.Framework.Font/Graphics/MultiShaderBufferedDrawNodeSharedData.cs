﻿// Copyright (c) andy840119 <andy840119@gmail.com>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Shaders;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics
{
    public class MultiShaderBufferedDrawNodeSharedData : BufferedDrawNodeSharedData
    {
        private readonly Dictionary<IShader, FrameBuffer> shaderBuffers = new Dictionary<IShader, FrameBuffer>();

        private readonly RenderbufferInternalFormat[] formats;

        public MultiShaderBufferedDrawNodeSharedData(RenderbufferInternalFormat[] formats = null, bool pixelSnapping = false)
            : base(0, formats, pixelSnapping)
        {
            this.formats = formats;
        }

        public void CreateDefaultFrameBuffers(IShader[] shaders)
        {
            if (shaderBuffers.Keys.SequenceEqual(shaders))
                return;

            var disposedFrameBuffer = shaderBuffers.Values.ToArray();
            shaderBuffers.Clear();

            foreach (var shader in shaders)
            {
                var filterMode = PixelSnapping ? All.Nearest : All.Linear;
                shaderBuffers.Add(shader, new FrameBuffer(formats, filterMode));
            }

            clearBuffersWithSchedule(() =>
            {
                clearBuffers(disposedFrameBuffer);
            });

            static void clearBuffersWithSchedule(Action action)
            {
                // should call GLWrapper.ScheduleDisposal(() => Dispose(true));
                Delegate act = new Action(() => action?.Invoke());
                var prop = typeof(GLWrapper).GetRuntimeMethods().FirstOrDefault(x => x.Name == "ScheduleDisposal");
                if (prop == null)
                    throw new NullReferenceException();

                prop.Invoke(prop, new object[] { act });
            }
        }

        public FrameBuffer GetSourceFrameBuffer(IShader shader)
        {
            if (!(shader is IStepShader stepShader))
                return CurrentEffectBuffer;

            var fromShader = stepShader.FromShader;
            if (fromShader == null)
                return CurrentEffectBuffer;

            if (!shaderBuffers.ContainsKey(fromShader))
                throw new DirectoryNotFoundException();

            return shaderBuffers[fromShader];
        }

        public FrameBuffer GetTargetFrameBuffer(IShader shader)
        {
            if (!shaderBuffers.ContainsKey(shader))
                throw new DirectoryNotFoundException();

            return shaderBuffers[shader];
        }

        public void UpdateBuffer(IShader shader, FrameBuffer frameBuffer)
        {
            if (!shaderBuffers.ContainsKey(shader))
                throw new Exception();

            shaderBuffers[shader] = frameBuffer;
        }

        public FrameBuffer[] GetDrawFrameBuffers()
            => shaderBuffers.Where(x =>
            {
                var shader = x.Key;
                if (shader is IStepShader stepShader)
                    return stepShader.StepShaders.Any() && stepShader.Draw;

                return true;
            }).Select(x => x.Value).ToArray();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            clearBuffers(shaderBuffers.Values.ToArray());
        }

        private void clearBuffers(FrameBuffer[] effectBuffers)
        {
            // dispose all frame buffer in array.
            foreach (var shaderBuffer in effectBuffers)
            {
                shaderBuffer.Dispose();
            }
        }
    }
}

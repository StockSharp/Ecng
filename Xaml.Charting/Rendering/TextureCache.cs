// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TextureCache.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the license, see http://www.ultrachart.com/ultrachart-eula/
// 
// This source code is protected by international copyright law. Unauthorized
// reproduction, reverse-engineering, or distribution of all or any portion of
// this source code is strictly prohibited.
// 
// This source code contains confidential and proprietary trade secrets of
// ulc software Services Ltd., and should at no time be copied, transferred, sold,
// distributed or made available without express written permission.
// *************************************************************************************
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Visuals.PointMarkers;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Rendering
{
    public class TextureCacheBase {
        internal readonly Dictionary<CharSpriteKey, ISprite2D> FontCache = new Dictionary<CharSpriteKey, ISprite2D>(); // 
        internal Dictionary<Tuple<string, float, FontWeight>, Size> MaxDigitSizeDict = new Dictionary<Tuple<string, float, FontWeight>, Size>();
    }

    /// <summary>
    /// The TextureCache is used by the <see cref="RenderSurfaceBase"/> to cache frequently used textures. Textures are used by 
    /// <see cref="FastMountainRenderableSeries"/>, <see cref="FastColumnRenderableSeries"/> where <see cref="LinearGradientBrush"/> is used and <see cref="SpritePointMarker"/>
    /// 
    /// </summary>
    public class TextureCache : TextureCacheBase
    {
        public static int MaxMemorySize = 1024 * 1024 * 32;
        public static int MaxItemsCount = 1024 * 2;

        private readonly LinkedList<Tuple<TextureKey, TextureType>> _textureKeys = new LinkedList<Tuple<TextureKey, TextureType>>(); // is used to store history of keys; to remove old entries

        private readonly Dictionary<TextureKey, byte[]> _cachedByteTextures = new Dictionary<TextureKey, byte[]>();

        private int _memorySize;

        public int MemorySize
        {
            get { return _memorySize; }
        }

        private void RemoveOldEntriesIfNeeded()
        {
            while (_memorySize > MaxMemorySize || _textureKeys.Count > MaxItemsCount)
            {
                Tuple<TextureKey, TextureType> oldKey = _textureKeys.First.Value;
                _textureKeys.RemoveFirst();

                if (oldKey.Item2 == TextureType.Byte)
                {
                    _memorySize -= _cachedByteTextures[oldKey.Item1].Length;
                    _cachedByteTextures.Remove(oldKey.Item1);
                }
                else if (oldKey.Item2 == TextureType.WriteableBitmap)
                {
                    WriteableBitmap wb = _cachedWriteableBitmapTextures[oldKey.Item1];
                    _memorySize -= wb.PixelHeight * wb.PixelWidth * 4;
                    _cachedWriteableBitmapTextures.Remove(oldKey.Item1);
                }
                else if (oldKey.Item2 == TextureType.Int)
                {
                    _memorySize -= _cachedIntTextures[oldKey.Item1].Length * 4;
                    _cachedIntTextures.Remove(oldKey.Item1);
                }
                else throw new Exception("unknown TextureType");
            }
        }

        public void AddTexture(Size size, Brush brush, byte[] texture)
        {
            var key = new TextureKey(size, brush);
            if (_cachedByteTextures.ContainsKey(key))
            {
                // there is already same texture in cache
                _memorySize -= _cachedByteTextures[key].Length;
                _cachedByteTextures[key] = texture;
                _memorySize += texture.Length;
                return;
            }
            _cachedByteTextures.Add(key, texture);
            _memorySize += texture.Length;
            _textureKeys.AddLast(new Tuple<TextureKey, TextureType>(key, TextureType.Byte));
            RemoveOldEntriesIfNeeded();
        }

        public byte[] GetByteTexture(Size size, Brush brush)
        {
            var key = new TextureKey(size, brush);
            byte[] r;
            if (!_cachedByteTextures.TryGetValue(key, out r)) return null;
            return r;
        }

        private readonly Dictionary<TextureKey, WriteableBitmap> _cachedWriteableBitmapTextures =
            new Dictionary<TextureKey, WriteableBitmap>();

        public WriteableBitmap GetWriteableBitmapTexture(FrameworkElement fe)
        {
            var key = new TextureKey(fe);
            WriteableBitmap r;
            if (!_cachedWriteableBitmapTextures.TryGetValue(key, out r))
            {
                r = fe.RenderToBitmap();
                _cachedWriteableBitmapTextures.Add(key, r);
                _memorySize += r.PixelWidth * r.PixelHeight * 4;
                _textureKeys.AddLast(new Tuple<TextureKey, TextureType>(key, TextureType.WriteableBitmap));
            }
            RemoveOldEntriesIfNeeded();
            return r;
        }

        private readonly Dictionary<TextureKey, int[]> _cachedIntTextures = new Dictionary<TextureKey, int[]>();

        public void AddTexture(Size size, Brush brush, int[] texture)
        {
            var key = new TextureKey(size, brush);

            if (_cachedIntTextures.ContainsKey(key))
            {
                // there is already same texture in cache
                _memorySize -= _cachedByteTextures[key].Length * 4;
                _cachedIntTextures[key] = texture;
                _memorySize += texture.Length * 4;
                return;
            }

            _cachedIntTextures.Add(key, texture);
            _memorySize += texture.Length * 4;
            _textureKeys.AddLast(new Tuple<TextureKey, TextureType>(key, TextureType.Int));
            RemoveOldEntriesIfNeeded();
        }

        public int[] GetIntTexture(Size size, Brush brush)
        {
            var key = new TextureKey(size, brush);
            int[] r;
            if (!_cachedIntTextures.TryGetValue(key, out r)) return null;
            return r;
        }

        private enum TextureType
        {
            Byte,
            Int,
            WriteableBitmap
        }
    }

    /// <summary>
    /// identifies cached rendered character
    /// </summary>
    class CharSpriteKey : IEquatable<CharSpriteKey> {
        public Color ForeColor { get; set; }
        public char Character { get; set; }
        public string FontFamily { get; set; }
        public FontWeight FontWeight { get; set; }
        public float FontSize {get; set;}

        public override int GetHashCode()
        {
            return ForeColor.GetHashCode() ^ Character.GetHashCode() ^ FontFamily.GetHashCode() ^ FontWeight.GetHashCode() ^ FontSize.GetHashCode();
        }

        public bool Equals(CharSpriteKey other) {
            if (other == null) return false;

            return 
                other.Character == this.Character && 
                other.ForeColor == this.ForeColor && 
                other.FontFamily == this.FontFamily && 
                other.FontWeight.Equals(this.FontWeight) &&
                other.FontSize == this.FontSize;
        }

        public override bool Equals(object obj) {
            return Equals(obj as CharSpriteKey);
        }
    }
}
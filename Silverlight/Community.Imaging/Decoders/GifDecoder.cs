using System.Collections.Generic;
using System.IO;
using Community.Imaging.Decoders.Gif;

namespace Community.Imaging.Decoders
{
    public class GifDecoder
    {
        private static void ReadImage(StreamHelper streamHelper, Stream fs, GifImage gifImage, IList<GraphicEx> graphics,
                                      int frameCount)
        {
            var imgDes = streamHelper.GetImageDescriptor(fs);
            var frame = new GifFrame();

            frame.ImageDescriptor = imgDes;
            frame.LocalColorTable = gifImage.GlobalColorTable;

            if(imgDes.LctFlag)
            {
                frame.LocalColorTable = streamHelper.ReadByte(imgDes.LctSize * 3);
            }

            var lzwDecoder = new LzwDecoder(fs);
            var dataSize = streamHelper.Read();

            frame.ColorDepth = dataSize;

            var pixels = lzwDecoder.DecodeImageData(imgDes.Width, imgDes.Height, dataSize);

            frame.IndexedPixel = pixels;

            var blockSize = streamHelper.Read();
            var data = new DataStruct(blockSize, fs);
            GraphicEx graphicEx = null;

            if(graphics.Count > 0)
            {
                graphicEx = graphics[frameCount];
            }

            frame.GraphicExtension = graphicEx;

            var img = GetImageFromPixel(pixels, frame.Palette, imgDes.InterlaceFlag, imgDes.Width, imgDes.Height);
            frame.Image = img;
            gifImage.Frames.Add(frame);
        }

        private static ClientImage GetImageFromPixel(byte[] pixels, Color32[] colorTable, bool interlactFlag,
                                                     int width, int height)
        {
            var img = new ClientImage(width, height);
            var idx = 0;

            for (var row = 0; row < height; row++)
            {
                for (var col = 0; col < width; col++)
                {
                    img.SetPixel(col, row, colorTable[pixels[idx++]].Color);
                }
            }
            return img;
        }

        public static GifImage Decode(Stream fs)
        {
            StreamHelper streamHelper = null;
            var gifImage = new GifImage();
            var graphics = new List<GraphicEx>();
            var frameCount = 0;

            streamHelper = new StreamHelper(fs);

            gifImage.Header = streamHelper.ReadString(6);
            gifImage.LogicalScreenDescriptor = streamHelper.GetLCD(fs);

            if(gifImage.LogicalScreenDescriptor.GlobalColorTableFlag)
            {
                gifImage.GlobalColorTable =
                    streamHelper.ReadByte(gifImage.LogicalScreenDescriptor.GlobalColorTableSize * 3);
            }

            var nextFlag = streamHelper.Read();

            while (nextFlag != 0)
            {
                if(nextFlag == GifExtensions.ImageLabel)
                {
                    ReadImage(streamHelper, fs, gifImage, graphics, frameCount);
                    frameCount++;
                }
                else if(nextFlag == GifExtensions.ExtensionIntroducer)
                {
                    var gcl = streamHelper.Read();
                    switch (gcl)
                    {
                        case GifExtensions.GraphicControlLabel:
                        {
                            var graphicEx = streamHelper.GetGraphicControlExtension(fs);
                            graphics.Add(graphicEx);
                            break;
                        }
                        case GifExtensions.CommentLabel:
                        {
                            var comment = streamHelper.GetCommentEx(fs);
                            gifImage.CommentExtensions.Add(comment);
                            break;
                        }
                        case GifExtensions.ApplicationExtensionLabel:
                        {
                            var applicationEx = streamHelper.GetApplicationEx(fs);
                            gifImage.ApplictionExtensions.Add(applicationEx);
                            break;
                        }
                        case GifExtensions.PlainTextLabel:
                        {
                            var textEx = streamHelper.GetPlainTextEx(fs);
                            gifImage.PlainTextEntensions.Add(textEx);
                            break;
                        }
                    }
                }
                else if(nextFlag == GifExtensions.EndIntroducer)
                {
                    break;
                }
                nextFlag = streamHelper.Read();
            }
            return gifImage;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CascScraper {
    public class DDSImage {
        private readonly Pfim.IImage _image;

        public byte[] Data {
            get {
                if (_image != null)
                    return _image.Data;
                else
                    return new byte[0];
            }
        }

        public DDSImage(string file) {
            _image = Pfim.Pfimage.FromFile(file);
            Process();
        }

        public DDSImage(Stream stream) {
            if (stream == null)
                throw new Exception("DDSImage ctor: Stream is null");

            _image = Pfim.Dds.Create(stream, new Pfim.PfimConfig());
            Process();
        }

        public DDSImage(byte[] data, bool treatAsFile = false) {
            if(treatAsFile) {
                using (var ms = new MemoryStream(data)) {
                    _image = Pfim.Pfimage.FromStream(ms);
                }
            }
            else {
                if (data == null || data.Length <= 0)
                    throw new Exception("DDSImage ctor: no data");

                _image = Pfim.Dds.Create(data, new Pfim.PfimConfig());
            }

            Process();
        }

        public void Save(string file) {
            if (_image.Format == Pfim.ImageFormat.Rgba32)
                Save<Bgra32>(file);
            else if (_image.Format == Pfim.ImageFormat.Rgb24)
                Save<Bgr24>(file);
            else if (_image.Format == Pfim.ImageFormat.R5g5b5a1)
                Save<Bgra5551>(file);
            else
                throw new Exception("Unsupported pixel format (" + _image.Format + ")");
        }

        private void Process() {
            if (_image == null)
                throw new Exception("DDSImage image creation failed");

            if (_image.Compressed)
                _image.Decompress();
        }

        private void Save<T>(string file)
            where T : struct, IPixel<T> {
            Image<T> image = Image.LoadPixelData<T>(
                _image.Data, _image.Width, _image.Height);
            image.Save(file);
        }
    }
}

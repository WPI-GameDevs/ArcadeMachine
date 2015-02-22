using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CaptureTest
{
    public partial class Form1 : Form
    {
        uint numAdapter = 0; // # of graphics card adapter
        uint numOutput = 1; // # of output device (i.e. monitor)

        SharpDX.Direct3D11.Device device;
        SharpDX.DXGI.Factory1 factory;
        SharpDX.Direct3D11.Texture2D screenTexture;
        SharpDX.DXGI.Output1 output;
        SharpDX.DXGI.OutputDuplication duplicatedOutput;

        Bitmap target;

        Stopwatch sw;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware);
            factory = new SharpDX.DXGI.Factory1();

            int width = factory.Adapters1[numAdapter].Outputs[numOutput].Description.DesktopBounds.Width;
            int height = factory.Adapters1[numAdapter].Outputs[numOutput].Description.DesktopBounds.Height;

            target = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // creating CPU-accessible texture resource
            SharpDX.Direct3D11.Texture2DDescription texdes = new SharpDX.Direct3D11.Texture2DDescription();
            texdes.CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.Read;
            texdes.BindFlags = SharpDX.Direct3D11.BindFlags.None;
            texdes.Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm;
            texdes.Height = height;
            texdes.Width = width;
            texdes.OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None;
            texdes.MipLevels = 1;
            texdes.ArraySize = 1;
            texdes.SampleDescription.Count = 1;
            texdes.SampleDescription.Quality = 0;
            texdes.Usage = SharpDX.Direct3D11.ResourceUsage.Staging;

            screenTexture = new SharpDX.Direct3D11.Texture2D(device, texdes);

            output = new SharpDX.DXGI.Output1(factory.Adapters1[numAdapter].Outputs[numOutput].NativePointer);
            duplicatedOutput = output.DuplicateOutput(device);
        }

        private void captureButton_Click(object sender, EventArgs e)
        {
            sw = new Stopwatch();
            sw.Start();

            captureButton.Enabled = false;
            displayTimer.Enabled = true;
        }

        private void dislpayTimer_Tick(object sender, EventArgs e)
        {     
             // duplicate output stuff
            SharpDX.DXGI.Resource screenResource = null;
            SharpDX.DataStream dataStream;
            SharpDX.DXGI.Surface screenSurface;

	        // try to get duplicated frame within given time
	        try
	        {
		        SharpDX.DXGI.OutputDuplicateFrameInformation duplicateFrameInformation;
		        duplicatedOutput.AcquireNextFrame(16, out duplicateFrameInformation, out screenResource);
	        }
	        catch (SharpDX.SharpDXException exc)
	        {
		        if (exc.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code)
		        {
			        // this has not been a successful capture
			        // thanks @Randy
 
			        // keep retrying
			        return;
		        }
		        else
		        {
			        throw exc;
		        }
	        }
 
	        // copy resource into memory that can be accessed by the CPU
	        device.ImmediateContext.CopyResource(screenResource.QueryInterface<SharpDX.Direct3D11.Resource>(), screenTexture);
	        // cast from texture to surface, so we can access its bytes
	        screenSurface = screenTexture.QueryInterface<SharpDX.DXGI.Surface>();
 
	        // map the resource to access it
	        screenSurface.Map(SharpDX.DXGI.MapFlags.Read, out dataStream);

            getImageFromDXStream(dataStream);

            captureBox.Image = target;
 
	        // free resources
	        dataStream.Close();
	        screenSurface.Unmap();
	        screenSurface.Dispose();
	        screenResource.Dispose();
	        duplicatedOutput.ReleaseFrame();
 
	        // print how many frames we could process within the last second
		    Console.WriteLine("fps: {0}", 1f / sw.Elapsed.TotalSeconds);
		    sw.Reset();
            sw.Start();


        }

        
        private void getImageFromDXStream(SharpDX.DataStream stream)
        {
            int Width = target.Width;
            int Height = target.Height;

	        var BoundsRect = new Rectangle(0, 0, Width, Height);
	        System.Drawing.Imaging.BitmapData bmpData = target.LockBits(BoundsRect, System.Drawing.Imaging.ImageLockMode.WriteOnly, target.PixelFormat);
	        int bytes = bmpData.Stride * target.Height;
 
	        var rgbValues = new byte[bytes];

            stream.Read(rgbValues, 0, bytes);
 
	        Marshal.Copy(rgbValues, 0, bmpData.Scan0, bytes);
	        target.UnlockBits(bmpData);
}
    }
}

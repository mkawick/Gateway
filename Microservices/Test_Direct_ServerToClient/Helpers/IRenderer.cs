using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace Core.PillarInterfaces
{
    public interface IRenderer
    {
        void Init( string data_folder );
        void BeginRendering();
        IntPtr GetRenderFrame(byte[] bytes, int len );

        void UpdateCameraMatrix( Matrix<float> matrix );
        void Shutdown();
    }
}

namespace Core.PillarInterfaces
{
    public class Renderer : IRenderer
    {
        private IRenderer RendererImpl;

        public Renderer()
        {
            //RendererImpl = new VulkanAssemblyRenderer();
        }

        public void Init(string data_folder) => RendererImpl.Init(data_folder);

        public void BeginRendering() => RendererImpl.BeginRendering();

        public IntPtr GetRenderFrame(byte[] bytes, int len) => RendererImpl.GetRenderFrame(bytes, len);

        public void UpdateCameraMatrix(Matrix<float> matrix) => RendererImpl.UpdateCameraMatrix(matrix);

        public void Shutdown() => RendererImpl.Shutdown();
    }
}

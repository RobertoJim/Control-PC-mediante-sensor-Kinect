using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    static class ClickMouse
    {
        [DllImport("user32")] //Se importa la biblioteca de enlace dinámico de windows

        public static extern int SetCursorPos(int x, int y); //Mueve el cursor a las coordenadas de pantalla especificadas

        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x0010;
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;

        [DllImport("user32.dll",
            CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]

        public static extern void mouse_event(int dwflags, int dx, int dy, int cButtons, int dwExtraInfo);

        private static void SetMouseState(int state)
        {
            int x = Cursor.Position.X;
            int y = Cursor.Position.Y;
            mouse_event(
                (int)state,
                (int)x,
                (int)y,
                0, 0);
        }

        public static void SetMouseLeftButtonDown()     //Presionar boton izquierdo
        {
            SetMouseState(MOUSEEVENTF_LEFTDOWN);
        }

        public static void SetMouseLeftButtonUp()   //Soltar boton izquierdo
        {
            SetMouseState(MOUSEEVENTF_LEFTUP);
        }

        public static void SetMouseRightButtonDown()    //Presionar boton derecho
        {
            SetMouseState(MOUSEEVENTF_RIGHTDOWN);
        }

        public static void SetMouseRightButtonUp()  //Soltar boton derecho
        {
            SetMouseState(MOUSEEVENTF_RIGHTUP);
        }

        public static void SetMouseMiddleButtonDown() //Presionar boton central
        {
            SetMouseState(MOUSEEVENTF_MIDDLEDOWN);
        }

        public static void SetMouseMiddleButtonUp() //Soltar boton central
        {
            SetMouseState(MOUSEEVENTF_MIDDLEUP);
        }
    }
}

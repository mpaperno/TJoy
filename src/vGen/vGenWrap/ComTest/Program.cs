using System;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Data;
//using System.Drawing;
using System.Text;
using vGenInterfaceWrap;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace ComTest
{

    class Program
    {
        static public vGen joystick;
        public vGen.JoystickState position;
        public uint id = 1;


        static void Main(string[] args)
        {
            Char last_c = '\0';
            uint id = 0;
            string line;
            DevType type = DevType.vJoy;
            int hDev=0;

            joystick = new vGen();

            // Print device' status
            dev_stat();

            // Get type of device
            Console.WriteLine("\n\nvJoy or vXbox [J/X]?");
            ConsoleKeyInfo key = Console.ReadKey();
            last_c = Char.ToUpper(key.KeyChar);
            if (last_c == 'X')
                type = DevType.vXbox;

            //get device ID
            Console.WriteLine("\n\nDevice ID [1-16]?");
            line = Console.ReadLine();
            if (line.Length==0)
            {
                Console.WriteLine("\n\nDevice ID <empty>, exiting?");
                return;
            }
            id = Convert.ToUInt32(line);

            // Verify ID
            if (id<1)
            {
                Console.WriteLine("\n\nDevice ID negative, exiting?");
                return;
            }
            if ((type == DevType.vXbox) && (id > 4))
            {
                Console.WriteLine("\n\nDevice ID is greater than 4, exiting?");
                return;
            }
            if ((type == DevType.vJoy) && (id > 16))
            {
                Console.WriteLine("\n\nDevice ID is greater than 16, exiting?");
                return;
            }

            // Acquire Device and verify
            joystick.AcquireDev(id, type, ref hDev);
            dev_stat();

            Console.WriteLine("\nHit any key to reliquish device");
            Console.ReadKey();
            joystick.RelinquishDev(hDev);
            dev_stat();

            Console.WriteLine("\nHit any key to re-acquire device");
            Console.ReadKey();
            joystick.AcquireDev(id, type, ref hDev);
            dev_stat();

            joystick.GetDevType(hDev, ref type);
            Console.WriteLine("\nDevice type: " +type.ToString());

            dev_axes(hDev);

            uint nBtn=0;
            joystick.GetDevButtonN(hDev, ref nBtn);
            Console.WriteLine("\nNumber of buttons: " + nBtn.ToString());

            uint nHat = 0;
            joystick.GetDevHatN(hDev, ref nHat);
            Console.WriteLine("\nNumber of Hats: " + nHat.ToString());

            bool go_on = true;
            do
            {
                go_on = SetVal( hDev);
            } while (go_on);

            Console.WriteLine("\nHit any key to exit");
            Console.ReadKey();

        }

        static void dev_stat()
        {
            bool Free=false, Owned=false, Exist=false;

            Console.WriteLine("\n\n\nXBOX Device:  1  2  3  4");
            Console.Write("              ");
            for (uint i=1; i<=4; i++)
            {
                joystick.isDevFree(i, DevType.vXbox, ref Free);
                if (Free)
                {
                    Console.Write("F  ");
                    continue;
                }
                joystick.isDevExist(i, DevType.vXbox, ref Exist);
                joystick.isDevOwned(i, DevType.vXbox, ref Owned);
                if (Exist && Owned)
                {
                    Console.Write("O  ");
                    continue;
                }
                if (Exist && !Owned)
                {
                    Console.Write("E  ");
                    continue;
                }

                Console.Write("?  ");

            }


            Console.WriteLine("\n\nvJoy Device:  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16");
            Console.Write("              ");
            for (uint i = 1; i <= 16; i++)
            {
                joystick.isDevFree(i, DevType.vJoy, ref Free);
                if (Free)
                {
                    Console.Write("F  ");
                    continue;
                }
                joystick.isDevExist(i, DevType.vJoy, ref Exist);
                joystick.isDevOwned(i, DevType.vJoy, ref Owned);
                if (Exist && Owned)
                {
                    Console.Write("O  ");
                    continue;
                }
                if (Exist && !Owned)
                {
                    Console.Write("B  ");
                    continue;
                }

                Console.Write("N  ");

            }

        }

        static void dev_axes(int hDev)
        {
            string[] j_axes = new string[8] { " X ", " Y ", " Z ", "Rx ", "Ry ", "Rz ", "S1 ", "S2 " };
            string[] x_axes = new string[6] { " X ", " Y ", "TR ", "Rx ", "Ry ", "TL " };
            bool Exist = false;
            DevType type = DevType.vJoy;
            int lim=6;

            if (type == DevType.vJoy)
                lim = 8;


            joystick.GetDevType(hDev, ref type);

            Console.WriteLine("");
            for (uint i=1; i<=lim;i++)
            {
                joystick.isAxisExist(hDev, i, ref Exist);
                if (Exist)
                {
                    if (type == DevType.vJoy)
                        Console.Write(j_axes[i - 1]);
                    else
                        Console.Write(x_axes[i - 1]);
                }
                else
                    Console.Write("   ");
            }
        }

        static bool SetVal(int hDev)
        {
            Console.WriteLine("\n [A]xis | [B]utton | [H]at | [Q]uit");
            ConsoleKeyInfo key = Console.ReadKey();
            char last_c = Char.ToUpper(key.KeyChar);
            switch (last_c)
            {
                case 'A':
                    SetAxis(hDev);
                    break;
                case 'B':
                    SetButton(hDev);
                    break;
                case 'H':
                    SetPov( hDev);
                    break;
                default:
                    return false;
            }
            return true;
        }

        static void SetAxis(int hDev)
        {
            Console.WriteLine("\nAxis number");
            ConsoleKeyInfo key = Console.ReadKey();

            Console.WriteLine("\nValue (0-100)");
            string s_val = Console.ReadLine();

            joystick.SetDevAxis(hDev, Convert.ToUInt32(key.KeyChar.ToString()), (float)Convert.ToDouble(s_val));
        }

        static void SetButton(int hDev)
        {
            Console.WriteLine("\nButton number");
            string s_Btn = Console.ReadLine();

            Console.WriteLine("\n[P]ress/[U]nPress");
            ConsoleKeyInfo key = Console.ReadKey();

            joystick.SetDevButton(hDev, Convert.ToUInt32(s_Btn), (Char.ToUpper(key.KeyChar) == 'P'));
        }

        static void SetPov(int hDev)
        {
            Console.WriteLine("\nPov number");
            ConsoleKeyInfo key = Console.ReadKey();
            string s_number = key.KeyChar.ToString();
            uint nPov = Convert.ToUInt32(s_number);


            Console.WriteLine("\nValue (0-359.99) or -1");
            string s_val = Console.ReadLine();
            float val = (float)Convert.ToDouble(s_val);

            joystick.SetDevPov(hDev, nPov, val);
        }


    }
}

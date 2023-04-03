#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using ClassicUO.Utility;
using static System.Net.Mime.MediaTypeNames;
using ClassicUO.Game.Managers;
using System.Runtime.CompilerServices;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.Utility.Collections;
using System.Diagnostics;
using static ClassicUO.Game.GameObjects.Mobile;
using ClassicUO.Game.Map;

namespace ClassicUO.Game.UI.Gumps
{

    internal class BoatMovementGump : AnchorableGump
    {
        public enum Orientation
        {
            North,
            East,
            South,
            West,
        }
        private struct BoatCommand
        {
            public int QueTime;
            public int Delay;
            public string command;
        }

        private static uint _timePacket;
        private static Deque<BoatCommand> _commands;
        private Orientation _orientation = Orientation.North;

        private (Direction direction, int graphic)[] _directions = new(Direction direction, int graphic)[]
        {
            new (Direction.Up, 4500),
            new (Direction.North, 4501),
            new (Direction.Right, 4502),
            new (Direction.East, 4503),
            new (Direction.Down, 4504),
            new (Direction.South, 4505),
            new (Direction.Left, 4506),
            new (Direction.West, 4507),
        };
        private HSliderBar _speed;
        private Combobox _orientationCombo;
        public Orientation CurrentOrientation
        {
            get => _orientation;
            set
            {
                _orientation = value;
                _orientationCombo.SelectedIndex = (int)value;
            }
        }
        public BoatMovementGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            Width = 200;
            Height = 270;
            _commands = new Deque<BoatCommand>();
        }

        public BoatMovementGump(int x, int y) : this()
        {
            X = x;
            Y = y;
          

            SetInScreen();

            BuildGump();
        }

        private void BuildGump()
        {
            WantUpdateSize = true;   
            Clear();

            Add
            (
                new AlphaBlendControl()
                {
                 Width = Width, Height = Height,
                }
            );

            Add(new Label("Step", true, 0xFF)
            {
                X = 10,
                Y = 10
            });
            Add(new Label("Slow", true, 0xFF)
            {
                X = (Width >> 1) - 10,
                Y = 10
            });
            Add(new Label("Fast", true, 0xFF)
            {
                X = Width - 35,
                Y = 10
            });

            Add(
                _speed = new HSliderBar(10,25,Width - 20, 0, 2, 1, HSliderBarStyle.MetalWidgetRecessedBar)
            );

            var rowY = 50;
            Add(BuildDirection(Direction.West, 25, rowY, out Rectangle bounds));
            Add(BuildDirection(Direction.Up, bounds.X + bounds.Width, rowY, out bounds));
            Add(BuildDirection(Direction.North, bounds.X + bounds.Width, rowY, out bounds));

            rowY = bounds.Y + bounds.Height;
            Add(BuildDirection(Direction.Left, 25, rowY, out bounds));

            var stop = new NiceButton(bounds.X + bounds.Width, rowY, bounds.Width, bounds.Height, ButtonAction.Activate, "STOP");
            stop.MouseUp += (sender, e) =>
            {
                stop.IsSelectable = false;
                HandleMovement(Direction.NONE, MouseButtonType.None);
            };
            Add(stop);
            bounds = stop.Bounds;
            Add(BuildDirection(Direction.Right, bounds.X + bounds.Width, rowY, out bounds));

            rowY = bounds.Y + bounds.Height;
            Add(BuildDirection(Direction.South, 25, rowY, out bounds));
            Add(BuildDirection(Direction.Down, bounds.X + bounds.Width, rowY, out bounds));
            Add(BuildDirection(Direction.East, bounds.X + bounds.Width, rowY, out bounds));

            Add(new Label("Boat Orientation", true, 0xFF)
            {
                X = 10,
                Y = bounds.Y + bounds.Height + 15
            });
            Add(_orientationCombo = new Combobox(10, bounds.Y + bounds.Height + 35, Width - 20, Enum.GetNames(typeof(Orientation))));
        }
        private HitBox BuildDirection(Direction direction, int x, int y, out Rectangle bounds)
        {
            var graphic = _directions.Where(o => o.direction == direction).Select(o => o.graphic).DefaultIfEmpty(0).FirstOrDefault();
            var texture = GumpsLoader.Instance.GetGumpTexture((uint)graphic, out Rectangle _bounds);
            if (texture == null)
            {
                bounds = Rectangle.Empty;
                return null;
            }
            var _hit = new HitBox(x,y,_bounds.Width,_bounds.Height);

            var _pic = new GumpPic(0, 0, (ushort)graphic, 0);

            _hit.MouseUp += (sender, args) =>
            {
                HandleMovement(direction, args.Button);
            };
            _hit.Add(_pic);
            _hit.X = x;
            _hit.Y = y;

            bounds = _hit.Bounds;
            return _hit;
        }


        protected override void UpdateContents()
        {
            BuildGump();
        }

        
        private bool OnBoat()
        {
            foreach (var h in World.HouseManager.Houses)
            {
                if (World.HouseManager.IsHouseInRange(h.Serial, 2))
                {
                    if (World.HouseManager.EntityIntoHouse(h.Serial, World.Player))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void SendCommand(string command, int delay = 0)
        {
            var _cmd = new BoatCommand() { command = command };
            _cmd.QueTime = (int)Time.Ticks;
            _cmd.Delay = delay;
            _commands.AddToBack(_cmd);
            _timePacket = Time.Ticks;
            
        }

        public void HandleMovement(Direction direction, MouseButtonType buttonType)
        {
            if (_commands.Any())
            {
                return;
            }
            if (direction == null || direction == Direction.NONE)
            {
                SendCommand("Stop");
                return;
            }

            if (new[] { Direction.Up, Direction.West }.Contains(direction) && buttonType == MouseButtonType.Right)
            {
                switch (_orientationCombo.SelectedIndex)
                {
                    case (int)Orientation.North:
                        SendCommand("Turn Left");
                        break;
                    case (int)Orientation.East:
                        SendCommand("Turn Left");
                        SendCommand("Turn Left", 1000);
                        break;
                    case (int)Orientation.South:
                        SendCommand("Turn Right");
                        break;
                    case (int)Orientation.West:
                        break;
                }

                CurrentOrientation = Orientation.West;
            }else if (new[] { Direction.Down, Direction.East }.Contains(direction) && buttonType == MouseButtonType.Right)
            {
                switch (_orientationCombo.SelectedIndex)
                {
                    case (int)Orientation.North:
                        SendCommand("Turn Right");
                        break;
                    case (int)Orientation.East:
                        break;
                    case (int)Orientation.South:
                        SendCommand("Turn Left");
                        break;
                    case (int)Orientation.West:
                        SendCommand("Turn Right");
                        SendCommand("Turn Right", 1000);
                        break;
                }

                CurrentOrientation = Orientation.East;
            }
            else if (new[] { Direction.Left, Direction.South }.Contains(direction) && buttonType == MouseButtonType.Right)
            {
                switch (_orientationCombo.SelectedIndex)
                {
                    case (int)Orientation.North:
                        SendCommand("Turn Left");
                        SendCommand("Turn Left", 1000);
                        break;
                    case (int)Orientation.East:
                        SendCommand("Turn Right");
                        break;
                    case (int)Orientation.South:
                        break;
                    case (int)Orientation.West:
                        SendCommand("Turn Left");
                        break;
                }

                CurrentOrientation = Orientation.South;
            }
            else if (new[] { Direction.Right, Direction.North }.Contains(direction) && buttonType == MouseButtonType.Right)
            {
                switch (_orientationCombo.SelectedIndex)
                {
                    case (int)Orientation.North:
                        break;
                    case (int)Orientation.East:
                        SendCommand("Turn Left");
                        break;
                    case (int)Orientation.South:
                        SendCommand("Turn Left");
                        SendCommand("Turn Left", 1000);
                        break;
                    case (int)Orientation.West:
                        SendCommand("Turn Right");
                        break;
                }

                CurrentOrientation = Orientation.North;
            }
            _timePacket = Time.Ticks;
        }

        public override void OnButtonClick(int buttonID)
        {
           if (!OnBoat())
            {
                return;
            }


            var speed = buttonID == 0 ? 0 : _speed.Value;
            BoatMovingManager.MoveRequest((Direction)buttonID, (byte)speed);
        }
        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (UIManager.MouseOverControl is HitBox && button == MouseButtonType.Right)
            {
                return;
            }
            base.OnMouseUp(x, y, button);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hue = ShaderHueTranslator.GetHueVector(0);
            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width - 3,
                Height + 1,
                hue
            );
            return base.Draw(batcher, x, y);
        }

        public override void Update()
        {
            var cleanList = new List<BoatCommand>();
            foreach(var _command in _commands)
            {
                if ((int)Time.Ticks - _command.QueTime >= _command.Delay)
                {
                    GameActions.Say(_command.command);
                    cleanList.Add(_command);
                }
            }

            if (cleanList.Any())
            {
                cleanList.ForEach(c => _commands.Remove(c));               
            }
           
        }

    }
}
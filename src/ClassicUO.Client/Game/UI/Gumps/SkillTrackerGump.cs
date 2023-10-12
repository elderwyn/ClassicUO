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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using System;
using ClassicUO.Configuration;
using static System.Windows.Forms.AxHost;
using System.Xml.Linq;

namespace ClassicUO.Game.UI.Gumps
{
    internal class SkillTrackerGump : Gump
    {
        private const int WIDTH = 254;
        public override GumpType GumpType => GumpType.SkillMenu;

        private ScrollArea area;
        private DataBox _dataBox;
        private bool _needsUpdate;

        public SkillTrackerGump() : base(0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = false;
            LayerOrder = UILayer.Default;

            Width = WIDTH;
            Height = 310;

            Add
            (new AlphaBlendControl(0.65f)
                {
                    X = 1,
                    Y = 1,
                    Width = WIDTH - 1,
                    Height = Height - 1
                }
            );

            area = new ScrollArea
            (
                5,
                5,
                WIDTH - 10,
                Height - 10,
                true
            )
            {
                AcceptMouseInput = true,
                CanMove = true,
            };

            Add(area);
            area.Add(_dataBox = new DataBox(0, 0, 1, 1)
            {
                WantUpdateSize = true
            });
            _needsUpdate = true;
        }

        private void BuildGump()
        {
            var skills = Enumerable.Range(0, SkillsLoader.Instance.SkillsCount).Select(i =>
            {
                return World.Player.Skills[i];
            }).OrderByDescending(s => Math.Max(s.Base, s.Value) / s.Cap).ToList();


            foreach (var s in skills)
            {
                _dataBox.Add(new SkillTrackerEntry(s));
            }

            _dataBox.WantUpdateSize = true;
            _dataBox.ReArrangeChildren();
            _needsUpdate = false;
        }
               
        public override void Update()
        {
            base.Update();
            if (_needsUpdate)
            {
                _dataBox.Clear();
                BuildGump();
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            batcher.DrawRectangle
            (
                SolidColorTextureCache.GetTexture(Color.Gray),
                x,
                y,
                Width,
                Height,
                hueVector
            );

            return base.Draw(batcher, x, y);
        }


        public void ForceUpdate()
        {
            _needsUpdate = true;
            Update();
        }

    }


    internal class SkillTrackerEntry : Control
    {
        private enum ProgressColors
        {
            RED = 0x0805,
            BLUE = 0x0806,
        }
        private readonly Skill _skill;
        public int Id => _skill == null ? 0 : _skill.GetHashCode();
        public float Percentage
        {
            get
            {
                if (_skill == null) { return 0; }
                return Math.Max(_skill.Base, _skill.Value) / _skill.Cap;
            }

        }
        public SkillTrackerEntry(Skill skill)
        {
            Height = 45;
            Width = 300;
            Label skillName = new Label(skill.Name, true, 1153, font: 3);
            GumpPic RedBar = new GumpPic(0, 0, (ushort)ProgressColors.RED, 0);
            GumpPicTiled ProgressBar = new GumpPicTiled(0, 0, 0, 0, (ushort)ProgressColors.BLUE);

            _skill = skill;
            CanMove = true;
            AcceptMouseInput = true;


            Add(skillName);

            RedBar.Y = skillName.Y + + skillName.Height + 5;
            Add(RedBar);

            GumpsLoader.Instance.GetGumpTexture((uint)ProgressColors.RED, out var barBounds);
            if (Percentage > 0)
            {
                ProgressBar.Y = RedBar.Y;
                ProgressBar.Width = (int)Math.Floor(barBounds.Width * Percentage);
                ProgressBar.Height = barBounds.Height;
                Add(ProgressBar);
            }
            else { 
            }
            if (_skill.Value > _skill.Base)
            {

            }

            var text = $"{_skill.Value} / {_skill.Cap}";
            var durWidth = FontsLoader.Instance.GetWidthUnicode(0, text);

            Add(new Label(text, true, 1153, font: 3)
            {
                Y = RedBar.Y - 2,
                X = barBounds.Width + 20
            });
        }

        protected override void OnDragBegin(int x, int y)
        {
            base.OnDragBegin(x, y);
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
        }

        protected override void OnMouseOver(int x, int y)
        {
            base.OnMouseOver(x, y);

        }

        private static SkillButtonGump GetSpellFloatingButton(int id)
        {
            for (LinkedListNode<Gump> i = UIManager.Gumps.Last; i != null; i = i.Previous)
            {
                if (i.Value is SkillButtonGump g && g.SkillID == id)
                {
                    return g;
                }
            }

            return null;
        }
    }
}
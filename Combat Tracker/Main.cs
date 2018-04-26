﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;

namespace Combat_Tracker
{

    public partial class Main : Form
    {
        private int characterIds = 0;
        private Roller roller;
        List<Character> combatants;

        public Main()
        {
            InitializeComponent();
            combatants = new List<Character>();
            roller = new Roller();
        }

        private void createCharacter_Click(object sender, EventArgs e)
        {
            Character newCharacter = new Character(
                    Interlocked.Increment(ref characterIds),
                    CharName.Text,
                    Convert.ToInt32(csInput.Text),
                    Convert.ToInt32(cpxInput.Text),
                    Convert.ToInt32(perInput.Text),
                    Convert.ToInt32(willInput.Text));
            newCharacter.InCombat = false;
            combatants.Add(newCharacter);

            characterStagingPanel.Controls.Add(CreateCharacterPanel(newCharacter));
        }

        private void unregister_Click(object sender, EventArgs e)
        {
            Button x = (Button)sender;
            combatants.ForEach(c =>
            {
                if (c.ID.ToString().Equals(x.Name))
                {
                    c.InCombat = false;
                    c.CombatRoll = -1;
                }
            });

            RedrawCharacterPanels();
        }

        private void register_Click(object sender, EventArgs e)
        {
            Button x = (Button)sender;
            combatants.ForEach(c =>
            {
                if (c.ID.ToString().Equals(x.Name))
                {
                    c.InCombat = true;
                    c.CombatRoll = 0;
                }
            });

            RedrawCharacterPanels();
        }

        private void startRound_Click(object sender, EventArgs e)
        {
            combatants.Where(c => c.InCombat)
                .ToList()
                .ForEach(c =>
                {
                    c.CombatRoll = roller.CalculateRoll(
                        c.Skill, c.Perception, c.Will, c.Wounds, c.Assist);
                });

            combatants.Where(c => !c.InCombat)
                .ToList()
                .ForEach(c =>
                {
                    c.CombatRoll = -1;
                });

            combatants = combatants.OrderByDescending(c => c.CombatRoll)
                .ThenByDescending(c => c.Complexity)
                .ThenByDescending(c => c.Perception)
                .ToList();

            RedrawCharacterPanels();
        }

        private void enterAllInCombat_Click(object sender, EventArgs e)
        {
            combatants.ForEach(c => { c.InCombat = true; });
            RedrawCharacterPanels();
        }

        private void clearToolAll_Click(object sender, EventArgs e)
        {
            combatants.Clear();
            RedrawCharacterPanels();
        }

        private void kO_Click(object sender, EventArgs e)
        {
            Button down = (Button)sender;
            combatants.Where(c => c.ID.ToString().Equals(down.Parent.Name))
                .ToList()
                .ForEach(c =>
                {
                    c.IsDown = true;
                });

            //TODO janky and I don't like it
            down.Parent.BackColor = Color.MistyRose;
            down.Click -= kO_Click;
            down.Click += new EventHandler(revive_Click);
            down.Text = "Revive";
        }

        private void revive_Click(object sender, EventArgs e)
        {
            Button down = (Button)sender;
            combatants.Where(c => c.ID.ToString().Equals(down.Parent.Name))
                .ToList()
                .ForEach(c => { c.IsDown = false; });

            //TODO janky and I don't like it
            down.Parent.BackColor = Color.WhiteSmoke;
            down.Click -= revive_Click;
            down.Click += new EventHandler(kO_Click);
            down.Text = "Down";
        }

        private void endCombat_Click(object sender, EventArgs e)
        {
            combatants.ForEach(c =>
            {
                c.InCombat = false;
                c.CombatRoll = 0;
            });
            RedrawCharacterPanels();
        }

        private void orderDown_Click(object sender, EventArgs e)
        {
            Button down = (Button)sender;
            Character temp = null;
            foreach (Character c in combatants.Where(c => c.InCombat))
            {
                if (down.Parent.Parent.Name == c.ID.ToString())
                {
                    temp = c;
                }
            }

            if (temp != null)
            {
                int index = combatants.IndexOf(temp);
                TrackerUtils.Swap(combatants, index, index + 1);
            }

            RedrawCharacterPanels();
        }

        private void orderUp_Click(object sender, EventArgs e)
        {
            Button up = (Button)sender;
            Character temp = null;
            foreach (Character c in combatants.Where(c => c.InCombat))
            {
                if (up.Parent.Parent.Name == c.ID.ToString())
                {
                    temp = c;
                }
            }

            if (temp != null)
            {
                int index = combatants.IndexOf(temp);
                TrackerUtils.Swap(combatants, index, index - 1);
            }

            RedrawCharacterPanels();
        }

        private void woundsTextbox_TextChanged(object sender, EventArgs e)
        {
            TextBox woundTextBox = (TextBox)sender;
            int wounds = 0;
            if (int.TryParse(woundTextBox.Text, out int n))
            {
                wounds = n;
            }

            // finds the combatant and adds wounds
            combatants.Where(c => c.ID.ToString().Equals(woundTextBox.Parent.Name))
                .ToList()
                .ForEach(c =>
                {
                    c.ApplyWounds(wounds);
                });
        }

        private Panel CreateCombatPanel(Character character)
        {
            Panel CombatPanel = new Panel();
            CombatPanel.Name = character.ID.ToString();
            CombatPanel.Location = new Point(10, 10);
            CombatPanel.BackColor = Color.WhiteSmoke;
            CombatPanel.Size = new Size(244, 75);
            CombatPanel.BorderStyle = BorderStyle.Fixed3D;

            Panel characterPanel = CreateCharacterPanel(character);
            GroupBox characterRollBox = CreateRollBox(character);
            CombatPanel.Controls.Add(characterPanel);
            CombatPanel.Controls.Add(characterRollBox);

            return CombatPanel;
        }

        private Panel CreateCharacterPanel(Character character)
        {
            Label characterName = new Label();
            characterName.Text = character.Name;
            characterName.Font = new Font("Sans Serif", 10, FontStyle.Bold);
            characterName.Location = new Point(3, 0);
            characterName.AutoSize = true;

            Label skillLBL = new Label();
            skillLBL.Text = "CS Skill";
            skillLBL.Margin = new Padding(0, 0, 0, 0);
            skillLBL.Padding = new Padding(0, 0, 0, 0);
            skillLBL.Location = new Point(6, 18);

            Label CharacterSkill = new Label();
            CharacterSkill.Text = character.Skill.ToString() + "/" + character.Complexity.ToString() + "+" + character.Perception.ToString();
            CharacterSkill.Location = new Point(48, 18);

            Label willLBL = new Label();
            willLBL.Text = "Will: " + character.Will.ToString();
            willLBL.Font = new Font("Sans Serif", 7, FontStyle.Regular);
            willLBL.Location = new Point(6, 32);

            Label modLBL = new Label();
            modLBL.Text = "Wounds:";
            modLBL.Font = new Font("Sans Serif", 7, FontStyle.Regular);
            modLBL.Location = new Point(45, 32);

            TextBox woundsTextbox = new TextBox();
            woundsTextbox.TextChanged += new EventHandler(woundsTextbox_TextChanged);
            woundsTextbox.Location = new Point(92, 28);
            woundsTextbox.Size = new Size(25, 5);
            woundsTextbox.BringToFront();

            Button register = CreateCombatButton(character.InCombat, character.ID.ToString());
            Button down = CreateKOButton(character.IsDown);
            Button remove = CreateRemoveButton();

            Panel newPanel = new Panel();
            newPanel.Name = character.ID.ToString();
            newPanel.Location = new Point(5, 6);
            newPanel.BackColor = character.IsDown ? Color.MistyRose : Color.WhiteSmoke;
            newPanel.Size = new Size(200, 64);
            newPanel.BorderStyle = BorderStyle.Fixed3D;
            newPanel.Controls.Add(down);
            newPanel.Controls.Add(register);
            newPanel.Controls.Add(woundsTextbox);
            newPanel.Controls.Add(modLBL);
            newPanel.Controls.Add(willLBL);
            newPanel.Controls.Add(CharacterSkill);
            newPanel.Controls.Add(skillLBL);
            newPanel.Controls.Add(characterName);
            newPanel.Controls.Add(remove);

            return newPanel;
        }

        private GroupBox CreateRollBox(Character character)
        {
            GroupBox rollBox = new GroupBox();
            rollBox.Size = new Size(30, 70);
            rollBox.Location = new Point(205, 0);

            Button up = new Button();
            up.Location = new Point(2, 10);
            up.Size = new Size(25, 15);
            up.BackgroundImage = Properties.Resources.up_arrow;
            up.BackgroundImageLayout = ImageLayout.Stretch;
            up.Click += new EventHandler(orderUp_Click);
            rollBox.Controls.Add(up);
            up.BringToFront();
            if (combatants.IndexOf(character) == 0)
            {
                up.Enabled = false;
            }

            Label combatRoll = new Label();
            combatRoll.Location = new Point(2, 30);
            combatRoll.Size = new Size(25, 15);
            combatRoll.Text = character.CombatRoll.ToString();
            combatRoll.Font = new Font("Sans Serif", 9, FontStyle.Bold);
            combatRoll.TextAlign = ContentAlignment.MiddleCenter;
            rollBox.Controls.Add(combatRoll);
            combatRoll.BringToFront();

            Button down = new Button();
            down.Location = new Point(2, 50);
            down.Size = new Size(25, 15);
            down.BackgroundImage = Properties.Resources.down_arrow;
            down.BackgroundImageLayout = ImageLayout.Stretch;
            down.Click += new EventHandler(orderDown_Click);
            rollBox.Controls.Add(down);
            down.BringToFront();
            if (combatants.IndexOf(character) == combatants.Count - 1)
            {
                down.Enabled = false;
            }

            return rollBox;
        }

        private Button CreateCombatButton(bool inCombat, string id)
        {
            Button register = new Button();
            register.Name = id;
            register.Font = new Font("Sans Serif", 7, FontStyle.Regular);
            register.Location = new Point(125, 3);
            register.Size = new Size(50, 20);
            register.BringToFront();

            if (inCombat)
            {
                register.Text = "Exit Combat";
                register.Click += new EventHandler(unregister_Click);
            }
            else
            {
                register.Text = "Enter Combat";
                register.Click += new EventHandler(register_Click);
            }
            return register;
        }

        private Button CreateKOButton(bool isDown)
        {
            Button down = new Button();
            down.Font = new Font("Sans Serif", 7, FontStyle.Regular);
            down.Location = new Point(125, 28);
            down.Size = new Size(50, 20);
            down.BringToFront();
            if (isDown)
            {
                down.Text = "Revive";
                down.Click += new EventHandler(revive_Click);
            }
            else
            {
                down.Text = "Down";
                down.Click += new EventHandler(kO_Click);
            }
            return down;
        }

        private Button CreateRemoveButton()
        {
            Button b = new Button()
            {
                Size = new Size(15, 15),
                Location = new Point(180, 0),
                BackgroundImage = Properties.Resources.close_button,
                BackgroundImageLayout = ImageLayout.Stretch,
            };
            b.Click += new EventHandler(remove_Click);
            b.BringToFront();
            return b;
        }

        private void RedrawCharacterPanels()
        {
            characterStagingPanel.Controls.Clear();
            characterCombatPanel.Controls.Clear();

            foreach (Character c in combatants)
            {
                if (c.InCombat)
                {
                    characterCombatPanel.Controls.Add(CreateCombatPanel(c));
                }
                else
                {
                    characterStagingPanel.Controls.Add(CreateCharacterPanel(c));
                }
            }
        }

        private void AddBatch(Character c)
        {
            characterStagingPanel.Controls.Add(CreateCharacterPanel(c));
        }

        private void saveCombatGroupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog fdlg = new SaveFileDialog();
            fdlg.Filter = "txt files (*.txt)|*.txt";


            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                StreamWriter sw = new StreamWriter(fdlg.FileName);
                {
                    if (File.Exists(fdlg.FileName))
                    {
                        foreach (Character x in combatants.Where(c => c.InCombat))
                        {
                            sw.WriteLine(x.Name);
                            sw.WriteLine(x.Skill);
                            sw.WriteLine(x.Complexity);
                            sw.WriteLine(x.Perception);
                            sw.WriteLine(x.Will);
                        }
                    }
                    sw.Close();
                }
            }
        }

        private void loadBatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Filter = "txt files (*.txt)|*.txt";
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                StreamReader sw = new StreamReader(fdlg.FileName);
                {
                    if (File.Exists(fdlg.FileName))
                    {
                        while (sw.Peek() >= 0)
                        {
                            Character current = new Character(
                                    Interlocked.Increment(ref characterIds),
                                    sw.ReadLine(),
                                    Convert.ToInt32(sw.ReadLine()),
                                    Convert.ToInt32(sw.ReadLine()),
                                    Convert.ToInt32(sw.ReadLine()),
                                    Convert.ToInt32(sw.ReadLine())
                            );
                            combatants.Add(current);
                            AddBatch(current);
                        }
                    }

                }
            }

        }

        private void remove_Click(object sender, EventArgs e)
        {
            Button b = (Button)sender;
            combatants.Where(c => c.ID.ToString().Equals(b.Parent.Name))
                    .ToList()
                    .ForEach(c => { combatants.Remove(c); });
            RedrawCharacterPanels();
        }
    }
}

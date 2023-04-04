//Copyright (C) 2016  John Hubert saintjohnny@gmail.com
//This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License for more details.
//You should have received a copy of the GNU Affero General Public License along with this program.  If not, see <http://www.gnu.org/licenses/>.

using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;//
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace pdfStamper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {

            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.WindowsShutDown) return;

            // Confirm user wants to close
            switch (MessageBox.Show(this, "Are you sure you want to close PDF Stamper?", "PDF Stamper", MessageBoxButtons.YesNo))
            {
                case DialogResult.No:
                    e.Cancel = true;
                    break;
                    default:
                    //on yes do:
                    //MessageBox.Show("Test");
                    this.axAcroPDF1.Dispose();
                    this.axAcroPDF1 = null;
                    break;
            }
        }

     
        //make sure program is not being run as administrator
        static bool IsElevated
        {
            get
            {
                return WindowsIdentity.GetCurrent().Owner
                  .IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
            }
        }

        string fontSelection;
        private void Form1_Load(object sender, EventArgs e)
        {
        

            createAppDataFolder();

            versionLabel.Visible = false;
            filesProcessedCountLabel.Text = "";
            filesProcessedLabel.Visible = false;


            string[] fileArray = Directory.GetFiles((Environment.GetFolderPath(Environment.SpecialFolder.Fonts)));

            foreach (string name in fileArray)
            {
                if (name.EndsWith(".TTF", StringComparison.InvariantCultureIgnoreCase))
                {
                    comboBoxFont.Items.Add(Path.GetFileName(name));
                }
            }


            try
            {
                comboBoxFont.SelectedIndex = comboBoxFont.FindStringExact("ARIALUNI.TTF");
            }
            catch (Exception)
            {

            }

            //default to first available font if arial can't be found
            if (comboBoxFont.Text.Equals("ARIALUNI.TTF", StringComparison.InvariantCultureIgnoreCase) == false)
            {
                comboBoxFont.SelectedIndex = 0;
            }

            fontSelection = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "ARIAL.TTF");

            string dllCheck = (Application.StartupPath + @"\" + "itextsharp.dll");
            if (!File.Exists(dllCheck))
            {


                labelDllError.Visible = true;
                MessageBox.Show(new Form() { TopMost = true }, @"itextsharp.dll not found.  That DLL file must be in the same directory as the pdfStamper.exe.  If you are running the program out of a zip folder, please unzip the folder first as the program cannot see the dll file when zipped.  Or if you lost the dll file just redownload from www.saintjohnny.com.",
                  "PDF Stamper Error",
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Exclamation);
            }


            string axinteropString = (Application.StartupPath + @"\" + "AxInterop.AcroPDFLib.dll");
            if (!File.Exists(axinteropString))
            {
                labelDllError.Visible = true;
                MessageBox.Show(new Form() { TopMost = true }, @"AxInterop.AcroPDFLib.dll not found.  That DLL file must be in the same directory as the pdfStamper.exe.  If you are running the program out of a zip folder, please unzip the folder first as the program cannot see the dll file when zipped.  Or if you lost the dll file just redownload from www.saintjohnny.com.",
                  "PDF Stamper Error",
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Exclamation);
            }

            string interopString = (Application.StartupPath + @"\" + "Interop.AcroPDFLib.dll");
            if (!File.Exists(interopString))
            {
                labelDllError.Visible = true;
                MessageBox.Show(new Form() { TopMost = true }, @"AxInterop.AcroPDFLib.dll not found.  That DLL file must be in the same directory as the pdfStamper.exe.  If you are running the program out of a zip folder, please unzip the folder first as the program cannot see the dll file when zipped.  Or if you lost the dll file just redownload from www.saintjohnny.com.",
                  "PDF Stamper Error",
                  MessageBoxButtons.OK,
                  MessageBoxIcon.Exclamation);
            }
            loadSettings();

            //make sure program is not run as administrator
            if (IsElevated == true)
            {
                labelAdmin.Visible = true;
                MessageBox.Show("Please close and reopen program without elevated admin privileges, windows does not allow drag and drop support when a program is run as admin.","PDF Stamper");
            }
            else
            {
               // MessageBox.Show("regular user");
            }
        }


        int errorCount = 0;
        private void stampProcess()
        {
            Application.DoEvents();

            refreshPreview.Enabled = true;

            string startupPath = Application.StartupPath;

            try
            {

                if (pdfCheck.EndsWith(".pdf"))
                {
                    if ((stampImage.Checked == true) && (imageBrowseText.Text == ""))
                    {
                        MessageBox.Show("Image stamp mode enabled without an image selected.  Please disable image stamp mode, or browse for an image to stamp", "Error");
                    }
                    else if ((!File.Exists(imageBrowseText.Text)) && (imageBrowseText.Text != "") && (stampImage.Checked == true))
                    {
                        MessageBox.Show("Image stamp file not found, please browse for a new image to stamp, or disable image stamp mode. The file path that no longer exists is: " + imageBrowseText.Text, "Error");
                    }

                    else
                    {

                        try
                        {
                            try
                            {
                                if (!Directory.Exists(dragDropPathwithoutPDF + @"\Labeled PDFs\"))
                                {
                                    // Try to create the directory.
                                    DirectoryInfo di = Directory.CreateDirectory(dragDropPathwithoutPDF + @"\Labeled PDFs\");
                                }
                            }
                            catch (IOException)
                            {

                                MessageBox.Show(new Form() { TopMost = true }, @"Error Creating Labeled PDF folder, is directory locked or write-protected?",
                                  "PDF Stamper Error",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Exclamation);
                            }

                            progressBar1.Maximum = fileCountInt;
                            progressBar1.Increment(1);

                            int tally = Convert.ToInt32(filesProcessedCountLabel.Text);

                            tally = tally + 1;
                            string tallyString = tally.ToString();
                            filesProcessedCountLabel.Text = tallyString;

                            /////////////
                            //GRAB UNICODE FONT SUPPORT


                            PdfReader reader = new PdfReader(dragDropPath);
                            PdfReader.unethicalreading = true;
                            PdfStamper stamper = new PdfStamper(reader, new FileStream(dragDropPathwithoutPDF + @"\Labeled PDFs\" + actualNameBeforeRemovingPDF, FileMode.Create));

                            int fontSize = Convert.ToInt32(numericFontSize.Value);
                            iTextSharp.text.Font font = new iTextSharp.text.Font(BaseFont.CreateFont(fontSelection, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED, Font.Bold));
                            font.Size = fontSize;


                            for (var i = 1; i <= reader.NumberOfPages; i++)
                            {
                                //determine if stamp every page, odd pages, or even pages, continue; will stop and go
                                //back to the top, not to be confused with break
                                if (radioButtonAllPages.Checked == true)
                                {
                                    //do nothing differently
                                }

                                if (radioButtonOddPages.Checked == true)
                                {
                                    if ((i % 2 == 0))//even
                                    {
                                        //continue here does odds
                                        continue;
                                    }
                                }

                                if (radioButtonEvenPages.Checked == true)
                                {
                                    //continue here does evens
                                    if ((i % 2 != 0))//odd
                                    {
                                        continue;
                                    }
                                }
                                if (radioButtonFirstPage.Checked == true)
                                {
                                    if (i != 1)//only stamp first page
                                    {
                                        continue;
                                    }
                                }
                                if (radioButtonLastPage.Checked == true)
                                {
                                    if (i != reader.NumberOfPages)//only stamp last page
                                    {
                                        continue;
                                    }
                                }

                                PdfContentByte pbunder = stamper.GetOverContent(i);
                                pbunder.SaveState();
                                pbunder.BeginText();

                                //begin color change
                                if (radioRed.Checked == true)
                                {
                                    pbunder.SetColorFill(iTextSharp.text.BaseColor.RED);
                                }
                                else if (radioBlue.Checked == true)
                                {
                                    pbunder.SetColorFill(iTextSharp.text.BaseColor.BLUE);
                                }
                                else if (radioGreen.Checked == true)
                                {
                                    pbunder.SetColorFill(iTextSharp.text.BaseColor.GREEN);
                                }
                                else if (radioBlack.Checked == true)
                                {
                                    pbunder.SetColorFill(iTextSharp.text.BaseColor.BLACK);
                                }
                                else if (radioWhite.Checked == true)
                                {
                                    pbunder.SetColorFill(iTextSharp.text.BaseColor.WHITE);
                                }

                                //end color change

                                pbunder.EndText();

                                //use largest page dimension for consistency of font size between portrait/landscape:
                                if (checkBoxAutoSizeFont.Checked == true)
                                {
                                    double findLargestDimension;

                                    iTextSharp.text.Rectangle mediaboxFontDetection = reader.GetPageSizeWithRotation(i);

                                    if (mediaboxFontDetection.Width > mediaboxFontDetection.Height)
                                    {
                                        findLargestDimension = mediaboxFontDetection.Width;
                                    }
                                    else
                                    {
                                        findLargestDimension = mediaboxFontDetection.Height;
                                    }

                                    double fontSizeDouble = (((double)(findLargestDimension / 72) / 100) * Convert.ToInt32(numericUpDownAutoSizeFont.Text));

                                    font.Size = Convert.ToInt32(fontSizeDouble);
                                    numericFontSize.Text = font.Size.ToString();
                                    fontSize = Convert.ToInt32(font.Size);
                                }


                                if (gravityBottomRadio.Checked == true)
                                {
                                    iTextSharp.text.Rectangle mediabox = reader.GetPageSizeWithRotation(i);
                                    //MessageBox.Show(mediabox.ToString());  //shows correct dimensions, need to convert pixels

                                    var widthBox = mediabox.Width;
                                    var TopBox = mediabox.Height;
                                   // MessageBox.Show(mediabox.Height.ToString());

                                    int heightPercentage = Convert.ToInt32(mediabox.Height / 100);
                                    int widthPercentage = Convert.ToInt32(mediabox.Width / 100);


                                    int xCoord = Convert.ToInt32(numericXcoordinates.Value * widthPercentage);
                                    int yCoord = Convert.ToInt32((numericYcoordinates.Value * heightPercentage));


                                    if (radioAlignLeft.Checked == true)
                                    {
                                        if ((checkBoxFilePath.Checked == false) && (checkBoxCustom.Checked == false) && (watermarkBox.Checked == false) && (checkBoxPageNumbers.Checked == false) && (checkBoxPageXofY.Checked == false))
                                        {

                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(prefixBox.Text + " " + actualName + " " + suffixBox.Text, new iTextSharp.text.Font(font))),
                                                               xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_LEFT);
                                            ct.Go();
                                        }

                                        else if ((checkBoxFilePath.Checked == true) && (watermarkBox.Checked == false))
                                        {

                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(prefixBox.Text + " " + dragDropPath + " " + suffixBox.Text, new iTextSharp.text.Font(font))),
                                                              xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_LEFT);
                                            ct.Go();
                                        }

                                        else if ((checkBoxCustom.Checked == true) && (watermarkBox.Checked == false))
                                        {
                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(textBoxCustom.Text, new iTextSharp.text.Font(font))),
                                                              xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_LEFT);
                                            ct.Go();

                                        }

                                        else if ((checkBoxPageNumbers.Checked == true) && (watermarkBox.Checked == false))
                                        {
                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(i.ToString(), new iTextSharp.text.Font(font))),
                                                              xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_LEFT);
                                            ct.Go();

                                        }
                                        else if ((checkBoxPageXofY.Checked == true) && (watermarkBox.Checked == false))
                                        {

                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(i.ToString() + " of " + reader.NumberOfPages.ToString(), new iTextSharp.text.Font(font))),
                                                              xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_LEFT);
                                            ct.Go();

                                        }
                                        else if (watermarkBox.Checked == true)
                                        {
                                            PdfGState gstate = new PdfGState();
                                            gstate.FillOpacity = 0.3f;

                                            pbunder.SaveState();
                                            pbunder.SetGState(gstate);

                                            int textCount = watermarkTextbox.Text.Length;
                                            ColumnText.ShowTextAligned(pbunder, Element.ALIGN_CENTER, new Phrase(watermarkTextbox.Text, new iTextSharp.text.Font(font)), widthBox / 2, TopBox / 2, 45);

                                            pbunder.SaveState();
                                            pbunder.SetGState(gstate);

                                        }
                                    }

                                    else if (radioAlignRight.Checked == true)
                                    {
                                        if ((checkBoxFilePath.Checked == false) && (checkBoxCustom.Checked == false) && (watermarkBox.Checked == false) && (checkBoxPageNumbers.Checked == false) && (checkBoxPageXofY.Checked == false) && (watermarkBox.Checked == false))
                                        {
                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(prefixBox.Text + " " + actualName + " " + suffixBox.Text, new iTextSharp.text.Font(font))),
                                                               xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_RIGHT);
                                            ct.Go();
                                        }

                                        else if ((checkBoxFilePath.Checked == true) && (watermarkBox.Checked == false))
                                        {

                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(prefixBox.Text + " " + dragDropPath + " " + suffixBox.Text, new iTextSharp.text.Font(font))),
                                                              xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_RIGHT);
                                            ct.Go();
                                        }

                                        else if ((checkBoxCustom.Checked == true) && (watermarkBox.Checked == false))
                                        {
                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(textBoxCustom.Text, new iTextSharp.text.Font(font))),
                                                              xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_RIGHT);
                                            ct.Go();

                                        }

                                        else if ((checkBoxPageNumbers.Checked == true) && (watermarkBox.Checked == false))
                                        {
                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(i.ToString(), new iTextSharp.text.Font(font))),
                                                              xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_RIGHT);
                                            ct.Go();

                                        }

                                        else if ((checkBoxPageXofY.Checked == true) && (watermarkBox.Checked == false))
                                        {
                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(i.ToString() + " of " + reader.NumberOfPages.ToString(), new iTextSharp.text.Font(font))),
                                                              xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_RIGHT);
                                            ct.Go();


                                        }
                                        else if (watermarkBox.Checked == true)
                                        {
                                            PdfGState gstate = new PdfGState();

                                            gstate.FillOpacity = 0.3f;

                                            pbunder.SaveState();
                                            pbunder.SetGState(gstate);

                                            int textCount = watermarkTextbox.Text.Length;
                                            ColumnText.ShowTextAligned(pbunder, Element.ALIGN_CENTER, new Phrase(watermarkTextbox.Text, new iTextSharp.text.Font(font)), widthBox / 2, TopBox / 2, 45);

                                            pbunder.SaveState();
                                            pbunder.SetGState(gstate);

                                        }
                                    }
                                    else if (radioAlignCenter.Checked == true)
                                    {
                                        if ((checkBoxFilePath.Checked == false) && (checkBoxCustom.Checked == false) && (watermarkBox.Checked == false) && (watermarkBox.Checked == false) && (checkBoxPageNumbers.Checked == false) && (checkBoxPageXofY.Checked == false))
                                        {
                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(prefixBox.Text + " " + actualName + " " + suffixBox.Text, new iTextSharp.text.Font(font))),
                                                               xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_CENTER);
                                            ct.Go();
                                        }

                                        else if ((checkBoxFilePath.Checked == true) && (watermarkBox.Checked == false))
                                        {

                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(prefixBox.Text + " " + dragDropPath + " " + suffixBox.Text, new iTextSharp.text.Font(font))),
                                                              xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_CENTER);
                                            ct.Go();
                                        }

                                        else if ((checkBoxCustom.Checked == true) && (watermarkBox.Checked == false))
                                        {
                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(textBoxCustom.Text, new iTextSharp.text.Font(font))),
                                                              xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_CENTER);
                                            ct.Go();

                                        }

                                        else if ((checkBoxPageNumbers.Checked == true) && (watermarkBox.Checked == false))
                                        {
                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(i.ToString(), new iTextSharp.text.Font(font))),
                                                              xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_CENTER);
                                            ct.Go();

                                        }

                                        else if ((checkBoxPageXofY.Checked == true) && (watermarkBox.Checked == false))
                                        {
                                            ColumnText ct = new ColumnText(pbunder);
                                            ct.SetSimpleColumn(new Phrase(new Chunk(i.ToString() + " of " + reader.NumberOfPages.ToString(), new iTextSharp.text.Font(font))),
                                                              xCoord, 10, widthBox - xCoord, yCoord + fontSize, fontSize, Element.ALIGN_CENTER);
                                            ct.Go();

                                        }
                                        else if (watermarkBox.Checked == true)
                                        {
                                            PdfGState gstate = new PdfGState();
                                            gstate.FillOpacity = 0.3f;

                                            pbunder.SaveState();
                                            pbunder.SetGState(gstate);

                                            int textCount = watermarkTextbox.Text.Length;
                                            ColumnText.ShowTextAligned(pbunder, Element.ALIGN_CENTER, new Phrase(watermarkTextbox.Text, new iTextSharp.text.Font(font)), widthBox / 2, TopBox / 2, 45);

                                            pbunder.SaveState();
                                            pbunder.SetGState(gstate);

                                        }
                                    }
                                    //last # [10] is for line spacing, which is optimized for font size 11

                                    pbunder.RestoreState();

                                }

                                if (gravityTopRadio.Checked == true)
                                {

                                    {
                                        iTextSharp.text.Rectangle mediabox = reader.GetPageSizeWithRotation(i);

                                        var widthBox = mediabox.Width;
                                        var TopBox = mediabox.Height;
                                        var bottomBox = mediabox.Bottom;

                                        int xCoord = Convert.ToInt32(numericXcoordinates.Value);
                                        int yCoord = Convert.ToInt32(numericYcoordinates.Value);

                                        if (radioAlignLeft.Checked == true)
                                        {

                                            if ((checkBoxFilePath.Checked == false) && (checkBoxCustom.Checked == false) && (watermarkBox.Checked == false) && (checkBoxPageNumbers.Checked == false) && (checkBoxPageXofY.Checked == false))
                                            {

                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(prefixBox.Text + " " + actualName + " " + suffixBox.Text, new iTextSharp.text.Font(font))),
                                                                   xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_LEFT);
                                                ct.Go();

                                            }

                                            else if ((checkBoxFilePath.Checked == true) && (watermarkBox.Checked == false))
                                            {

                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(prefixBox.Text + " " + dragDropPath + " " + suffixBox.Text, new iTextSharp.text.Font(font))),
                                                                  xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_LEFT);
                                                ct.Go();
                                            }

                                            else if ((checkBoxCustom.Checked == true) && (watermarkBox.Checked == false))
                                            {
                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(textBoxCustom.Text, new iTextSharp.text.Font(font))),
                                                                  xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_LEFT);
                                                ct.Go();

                                            }

                                            else if ((checkBoxPageNumbers.Checked == true) && (watermarkBox.Checked == false))
                                            {
                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(i.ToString(), new iTextSharp.text.Font(font))),
                                                                  xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_LEFT);
                                                ct.Go();

                                            }

                                            else if ((checkBoxPageXofY.Checked == true) && (watermarkBox.Checked == false))
                                            {
                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(i.ToString() + " of " + reader.NumberOfPages.ToString(), new iTextSharp.text.Font(font))),
                                                                  xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_LEFT);
                                                ct.Go();

                                            }

                                            else if (watermarkBox.Checked == true)
                                            {
                                                PdfGState gstate = new PdfGState();

                                                gstate.FillOpacity = 0.3f;

                                                pbunder.SaveState();
                                                pbunder.SetGState(gstate);

                                                int textCount = watermarkTextbox.Text.Length;
                                                ColumnText.ShowTextAligned(pbunder, Element.ALIGN_CENTER, new Phrase(watermarkTextbox.Text, new iTextSharp.text.Font(font)), widthBox / 2, TopBox / 2, 45);

                                                pbunder.SaveState();
                                                pbunder.SetGState(gstate);

                                            }
                                        }

                                        else if (radioAlignRight.Checked == true)
                                        {

                                            if ((checkBoxFilePath.Checked == false) && (checkBoxCustom.Checked == false) && (watermarkBox.Checked == false) && (checkBoxPageNumbers.Checked == false) && (checkBoxPageXofY.Checked == false))
                                            {
                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(prefixBox.Text + " " + actualName + " " + suffixBox.Text, new iTextSharp.text.Font(font))),
                                                                   xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_RIGHT);
                                                ct.Go();

                                            }


                                            else if ((checkBoxFilePath.Checked == true) && (watermarkBox.Checked == false))
                                            {
                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(prefixBox.Text + " " + dragDropPath + " " + suffixBox.Text, new iTextSharp.text.Font(font))),
                                                                  xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_RIGHT);
                                                ct.Go();
                                            }

                                            else if ((checkBoxCustom.Checked == true) && (watermarkBox.Checked == false))
                                            {
                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(textBoxCustom.Text, new iTextSharp.text.Font(font))),
                                                                  xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_RIGHT);
                                                ct.Go();

                                            }

                                            else if ((checkBoxPageNumbers.Checked == true) && (watermarkBox.Checked == false))
                                            {

                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(i.ToString(), new iTextSharp.text.Font(font))),
                                                                  xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_RIGHT);
                                                ct.Go();

                                            }

                                            else if ((checkBoxPageXofY.Checked == true) && (watermarkBox.Checked == false))
                                            {
                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(i.ToString() + " of " + reader.NumberOfPages.ToString(), new iTextSharp.text.Font(font))),
                                                                  xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_RIGHT);
                                                ct.Go();

                                            }
                                            else if (watermarkBox.Checked == true)
                                            {
                                                PdfGState gstate = new PdfGState();

                                                gstate.FillOpacity = 0.3f;

                                                pbunder.SaveState();
                                                pbunder.SetGState(gstate);

                                                int textCount = watermarkTextbox.Text.Length;
                                                ColumnText.ShowTextAligned(pbunder, Element.ALIGN_CENTER, new Phrase(watermarkTextbox.Text, new iTextSharp.text.Font(font)), widthBox / 2, TopBox / 2, 45);

                                                pbunder.SaveState();
                                                pbunder.SetGState(gstate);

                                            }
                                        }

                                        else if (radioAlignCenter.Checked == true)
                                        {

                                            if ((checkBoxFilePath.Checked == false) && (checkBoxCustom.Checked == false) && (watermarkBox.Checked == false) && (checkBoxPageNumbers.Checked == false) && (checkBoxPageXofY.Checked == false))
                                            {

                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(prefixBox.Text + " " + actualName + " " + suffixBox.Text, new iTextSharp.text.Font(font))),
                                                                   xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_CENTER);
                                                ct.Go();
                                            }


                                            else if ((checkBoxFilePath.Checked == true) && (watermarkBox.Checked == false))
                                            {

                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(prefixBox.Text + " " + dragDropPath + " " + suffixBox.Text, new iTextSharp.text.Font(font))),
                                                                  xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_CENTER);
                                                ct.Go();
                                            }

                                            else if ((checkBoxCustom.Checked == true) && (watermarkBox.Checked == false))
                                            {
                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(textBoxCustom.Text, new iTextSharp.text.Font(font))),
                                                                  xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_CENTER);
                                                ct.Go();

                                            }

                                            else if ((checkBoxPageNumbers.Checked == true) && (watermarkBox.Checked == false))
                                            {

                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(i.ToString(), new iTextSharp.text.Font(font))),
                                                                  xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_CENTER);
                                                ct.Go();

                                            }

                                            else if ((checkBoxPageXofY.Checked == true) && (watermarkBox.Checked == false))
                                            {
                                                ColumnText ct = new ColumnText(pbunder);
                                                ct.SetSimpleColumn(new Phrase(new Chunk(i.ToString() + " of " + reader.NumberOfPages.ToString(), new iTextSharp.text.Font(font))),
                                                                  xCoord, 10, widthBox - xCoord, TopBox - fontSize / 1000, fontSize, Element.ALIGN_CENTER);
                                                ct.Go();

                                            }
                                            else if (watermarkBox.Checked == true)
                                            {

                                                PdfGState gstate = new PdfGState();

                                                gstate.FillOpacity = 0.3f;

                                                pbunder.SaveState();
                                                pbunder.SetGState(gstate);

                                                int textCount = watermarkTextbox.Text.Length;
                                                ColumnText.ShowTextAligned(pbunder, Element.ALIGN_CENTER, new Phrase(watermarkTextbox.Text, new iTextSharp.text.Font(font)), widthBox / 2, TopBox / 2, 45);

                                                pbunder.SaveState();
                                                pbunder.SetGState(gstate);

                                            }
                                        }

                                        pbunder.RestoreState();
                                    }

                                }///

                                ////stamp image to pdf start
                                int imageXCoord = Convert.ToInt32(imageXcoordinates.Value);
                                int imageYCoord = Convert.ToInt32(imageYcoordinates.Value);
                                int imageSizePercentageInt = Convert.ToInt32(imageSizePercentage.Value);

                                if (stampImage.Checked == true)
                                {

                                    if (stampImage.Checked == true)
                                    {
                                        PdfGState gstate = new PdfGState();

                                        iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(imageBrowseText.Text);

                                        image.ScalePercent(imageSizePercentageInt);
                                        image.SetAbsolutePosition(imageXCoord, imageYCoord);

                                        pbunder.AddImage(image);

                                        pbunder.SaveState();
                                        pbunder.SetGState(gstate);
                                    }

                                }
                                //////stamp image to pdf end

                            }
                            // stamper.SetFullCompression();
                            stamper.Close();

                        }
                        catch (Exception ex)
                        {

                            MessageBox.Show(new Form() { TopMost = true }, @"Error, please make sure file is a PDF and is not currently opened and locked: " + ex,
                                          "PDF Stamper Error",
                                          MessageBoxButtons.OK,
                                          MessageBoxIcon.Exclamation);
                        }
                    }
                }

                else
                {

                    if (errorCount == 0)
                    {
                        errorCount = 1;

                        MessageBox.Show(new Form() { TopMost = true }, "Program supports PDF files only",
                                        "PDF Stamper Error",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Exclamation);

                    }

                }

            }

            catch (Exception)
            {

            }
            errorCount = 0;

        }


        int fileCountInt;

        string dragDropPath;
        string actualName;
        string actualNameBeforeRemovingPDF;
        string pdfCheck;
        string dragDropPathwithoutPDF;

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {

            fileCountInt = 0;

            progressBar1.Value = 0;
            filesProcessedLabel.Visible = true;
            filesProcessedCountLabel.Text = "0";

            string[] FileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);

            foreach (var fileCount in FileList)
            {
                fileCountInt = (fileCountInt + 1);
            }


            foreach (string FileStuff in FileList)
            {
                dragDropPath = FileStuff;

                dragDropPathwithoutPDF = Path.GetDirectoryName(dragDropPath);

                actualNameBeforeRemovingPDF = (Path.GetFileName(dragDropPath));

                actualName = Regex.Replace(actualNameBeforeRemovingPDF, ".pdf", "", RegexOptions.IgnoreCase);
                pdfCheck = Regex.Replace(actualNameBeforeRemovingPDF, ".pdf", ".pdf", RegexOptions.IgnoreCase);

                stampProcess();

            }

            try
            {
                axAcroPDF1.src = (dragDropPathwithoutPDF + @"\Labeled PDFs\" + actualNameBeforeRemovingPDF);

                this.axAcroPDF1.setShowToolbar(false);
                this.axAcroPDF1.setPageMode("False");
                this.axAcroPDF1.setView("fit");
            }
            catch (Exception)
            {

            }

            Process.Start(dragDropPathwithoutPDF + @"\Labeled PDFs\");

        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("www.saintjohnny.com");
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            string url = "";
            string business = "saintjohnny@gmail.com";  // your paypal email
            string description = "Saintjohnny's Software Tools";
            string country = "US";                  // AU, US, etc.
            string currency = "USD";                 // AUD, USD, etc.

            url += "https://www.paypal.com/cgi-bin/webscr" +
                "?cmd=" + "_donations" +
                "&business=" + business +
                "&lc=" + country +
                "&item_name=" + description +
                "&currency_code=" + currency +
                "&bn=" + "PP%2dDonationsBF";

            System.Diagnostics.Process.Start(url);

        }

        private void checkBoxFilePath_CheckedChanged(object sender, EventArgs e)
        {

            if (checkBoxFilePath.Checked == true)
            {
                checkBoxCustom.Checked = false;
            }

        }

        private void checkBoxCustom_CheckedChanged(object sender, EventArgs e)
        {

            {
                if (checkBoxCustom.Checked == true)
                {
                    textBoxCustom.Enabled = true;
                    checkBoxFilePath.Checked = false;
                    prefixBox.Enabled = false;
                    suffixBox.Enabled = false;
                    watermarkBox.Enabled = false;
                    watermarkTextbox.Enabled = false;
                    checkBoxFilePath.Enabled = false;
                    textBoxCustom.BackColor = Color.Yellow;
                    checkBoxPageNumbers.Enabled = false;
                    checkBoxPageXofY.Enabled = false;
                    checkBoxPageNumbers.Checked = false;
                    checkBoxPageXofY.Checked = false;

                }

                else if (checkBoxCustom.Checked == false)
                {
                    textBoxCustom.Enabled = false;

                    prefixBox.Enabled = true;
                    suffixBox.Enabled = true;
                    watermarkBox.Enabled = true;
                    checkBoxFilePath.Enabled = true;
                    checkBoxPageNumbers.Enabled = true;
                    checkBoxPageXofY.Enabled = true;
                    textBoxCustom.BackColor = SystemColors.Window;
                }
            }


        }

        private void watermarkBox_CheckedChanged(object sender, EventArgs e)
        {


            if (watermarkBox.Checked == true)
            {
                textBoxCustom.Enabled = false;
                checkBoxCustom.Enabled = false;
                prefixBox.Enabled = false;
                suffixBox.Enabled = false;
                numericXcoordinates.Enabled = false;
                numericYcoordinates.Enabled = false;
                checkBoxFilePath.Enabled = false;
                checkBoxCustom.Checked = false;
                watermarkTextbox.Enabled = true;
                radioAlignCenter.Enabled = false;
                radioAlignLeft.Enabled = false;
                gravityBottomRadio.Enabled = false;
                gravityTopRadio.Enabled = false;
                radioAlignRight.Enabled = false;
                checkBoxPageNumbers.Enabled = false;
                checkBoxPageXofY.Enabled = false;
                checkBoxPageNumbers.Checked = false;
                checkBoxPageXofY.Checked = false;
                watermarkTextbox.BackColor = Color.Yellow;
            }

            else if (watermarkBox.Checked == false)
            {
                textBoxCustom.Enabled = false;

                checkBoxCustom.Enabled = true;
                prefixBox.Enabled = true;
                suffixBox.Enabled = true;
                numericXcoordinates.Enabled = true;
                numericYcoordinates.Enabled = true;
                checkBoxFilePath.Enabled = true;
                watermarkTextbox.Enabled = false;
                radioAlignCenter.Enabled = true;
                radioAlignLeft.Enabled = true;
                gravityBottomRadio.Enabled = true;
                gravityTopRadio.Enabled = true;
                watermarkTextbox.BackColor = SystemColors.Window;
                radioAlignRight.Enabled = true;
                checkBoxPageNumbers.Enabled = true;
                checkBoxPageXofY.Enabled = true;

            }


        }



        private void button2_Click(object sender, EventArgs e)
        {
            versionLabel.Visible = true;
        }

        private void gravityBottomRadio_CheckedChanged(object sender, EventArgs e)
        {
            numericYcoordinates.Enabled = true;
        }

        private void gravityTopRadio_CheckedChanged(object sender, EventArgs e)
        {
            numericYcoordinates.Enabled = false;
        }

        int customWarning = 1;

        private void stampImage_CheckedChanged(object sender, EventArgs e)
        {

            if (stampImage.Checked == true)
            {

                imageBrowseButton.Enabled = true;
                imageXcoordinates.Enabled = true;
                imageYcoordinates.Enabled = true;
                imageSizePercentage.Enabled = true;
                checkBoxPageXofY.Enabled = false;
                checkBoxPageXofY.Checked = false;
                checkBoxPageNumbers.Enabled = false;
                checkBoxPageNumbers.Checked = false;
                checkBoxFilePath.Enabled = false;
                checkBoxFilePath.Checked = false;
                watermarkBox.Enabled = false;
                watermarkBox.Checked = false;
                checkBoxCustom.Checked = true;
                textBoxCustom.BackColor = Color.Yellow;

                groupBox8.Visible = true;


                if (customWarning == 1)
                {//so warning only pops up once per session and does not annoy user
                    MessageBox.Show("Enabling custom text mode so that no unwanted filename text shows up with your image stamp.  If you would like to also include filename text feel free to uncheck the custom text mode checkbox.  This warning will only pop up once per session as a reminder.", "Image Stamp Mode");
                    customWarning = 0;
                }
            }

            else
            {

                imageBrowseButton.Enabled = false;
                imageXcoordinates.Enabled = false;
                imageYcoordinates.Enabled = false;
                imageSizePercentage.Enabled = false;
                checkBoxCustom.Checked = false;



            }
        }

        private void imageBrowseButton_Click(object sender, EventArgs e)
        {

            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "Image files (*.jpg; *.jpeg; *.tiff; *.jb2; *.png; *.wmf; *.bmp; *.gif; *.tif) | *.jpg; *.jpeg; *.tiff; *.jb2; *.png; *.wmf; *.bmp; *.gif; *.tif";
                dialog.ShowDialog();

                string sFileName = dialog.FileName;
                imageBrowseText.Text = ("");
                imageBrowseText.Text += sFileName;

                if (imageBrowseText.Text == "")
                {
                    MessageBox.Show("No image file was selected.  Disabling Image Stamp mode and custom text mode", "Image Stamp Mode");
                    stampImage.Checked = false;
                    checkBoxCustom.Checked = false;
                }
            }
        }

        private void resetToFactorSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            radioAlignLeft.Checked = true;
            radioRed.Checked = true;
            numericFontSize.Text = "11";
            numericXcoordinates.Text = "3";
            numericYcoordinates.Text = "3";
            watermarkBox.Checked = false;
            checkBoxFilePath.Checked = false;
            checkBoxCustom.Checked = false;
            gravityBottomRadio.Checked = true;
            stampImage.Checked = false;
            imageXcoordinates.Text = "5";
            imageYcoordinates.Text = "5";
            imageSizePercentage.Text = "100";
            imageBrowseText.Text = "";
            prefixBox.Text = "";
            suffixBox.Text = "";
            textBoxCustom.Text = "";
            watermarkTextbox.Text = "";
            radioButtonAllPages.Checked = true;
            checkBoxAutoSizeFont.Checked = true;
            numericUpDownAutoSizeFont.Text = "100";

            //reset font
            try
            {
                comboBoxFont.SelectedIndex = comboBoxFont.FindStringExact("ARIALUNI.TTF");
            }
            catch (Exception)
            {

            }
            //default to first available font if arial can't be found
            if (comboBoxFont.Text.Equals("ARIALUNI.TTF", StringComparison.InvariantCultureIgnoreCase) == false)
            {
                comboBoxFont.SelectedIndex = 0;
            }


        }

        private void createAppDataFolder()
        {

            try
            {
                appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                appDataStamperFolder = Path.Combine(appDataDirectory, "PDF Stamper");


                if (!Directory.Exists(appDataStamperFolder))
                    Directory.CreateDirectory(appDataStamperFolder);
            }
            catch
            { }
        }

        string appDataDirectory;
        string appDataStamperFolder;

        private void writeToConfigFile()
        {

            using (StreamWriter sw = File.CreateText(appDataStamperFolder + "\\" + "PDF Stamper.cfg"))
            {
                //radio buttons start
                if (radioAlignLeft.Checked == true)
                {
                    sw.WriteLine("radioAlignLeft checked");
                }
                if (radioAlignCenter.Checked == true)
                {
                    sw.WriteLine("radioAlignCenter checked");
                }
                if (radioAlignRight.Checked == true)
                {
                    sw.WriteLine("radioAlignRight checked");
                }
                if (radioRed.Checked == true)
                {
                    sw.WriteLine("radioRed checked");
                }
                if (radioBlack.Checked == true)
                {
                    sw.WriteLine("radioBlack checked");
                }
                if (radioBlue.Checked == true)
                {
                    sw.WriteLine("radioBlue checked");
                }
                if (radioGreen.Checked == true)
                {
                    sw.WriteLine("radioGreen checked");
                }
                if (radioWhite.Checked == true)
                {
                    sw.WriteLine("radioWhite checked");
                }
                if (gravityTopRadio.Checked == true)
                {
                    sw.WriteLine("gravityTopRadio checked");
                }
                if (gravityBottomRadio.Checked == true)
                {
                    sw.WriteLine("gravityBottomRadio checked");
                }
                if (radioButtonAllPages.Checked == true)
                {
                    sw.WriteLine("radioButtonAllPages");
                }
                if (radioButtonOddPages.Checked == true)
                {
                    sw.WriteLine("radioButtonOddPages");
                }
                if (radioButtonEvenPages.Checked == true)
                {
                    sw.WriteLine("radioButtonEvenPages");
                }
                if (radioButtonFirstPage.Checked == true)
                {
                    sw.WriteLine("radioButtonFirstPage");
                }
                if (radioButtonLastPage.Checked == true)
                {
                    sw.WriteLine("radioButtonLastPage");
                }
                //radio buttons end

                //regular check boxes start
                if (watermarkBox.Checked == true)
                {
                    sw.WriteLine("watermarkBox checked");
                }
                if (checkBoxFilePath.Checked == true)
                {
                    sw.WriteLine("checkBoxFilePath checked");
                }
                if (checkBoxCustom.Checked == true)
                {
                    sw.WriteLine("checkBoxCustom checked");
                }

                if (stampImage.Checked == true)
                {
                    sw.WriteLine("stampImage checked");
                }
                if (checkBoxPageNumbers.Checked == true)
                {
                    sw.WriteLine("checkBoxPageNumbers checked");
                }
                if (checkBoxPageXofY.Checked == true)
                {
                    sw.WriteLine("checkBoxPageXofY checked");
                }
                if (checkBoxAutoSizeFont.Checked == true)
                {
                   
                    sw.WriteLine("checkBoxAutoSizeFont Checked");
                }
                if (checkBoxAutoSizeFont.Checked == false)
                {

                    sw.WriteLine("checkBoxAutoSizeFont UnChecked");
                }

                try
                {
                    sw.WriteLine("comboBoxFont " + comboBoxFont.Text);
                }
                catch (Exception)
                {
                    MessageBox.Show("error writing font to config file, is it blank?");
                }

                //regular check boxes end

                //numeric controls begin
                //<b> ,</b>

                sw.WriteLine("numericFontSize " + numericFontSize.Text);

                sw.WriteLine("numericXcoordinates " + numericXcoordinates.Text);

                sw.WriteLine("numericYcoordinates " + numericYcoordinates.Text);

                //image parts
                sw.WriteLine("imageXcoordinates " + imageXcoordinates.Text);
                sw.WriteLine("imageYcoordinates " + imageYcoordinates.Text);
                sw.WriteLine("imageSizePercentage " + imageSizePercentage.Text);
                sw.WriteLine("numericUpDownAutoSizeFont " + numericUpDownAutoSizeFont.Text);

                //numeric controls end

                //textboxes begin
                sw.WriteLine("prefixBox " + prefixBox.Text);
                sw.WriteLine("suffixBox " + suffixBox.Text);
                sw.WriteLine("textBoxCustom " + textBoxCustom.Text);
                sw.WriteLine("watermarkTextbox " + watermarkTextbox.Text);
                sw.WriteLine("imageBrowseText " + imageBrowseText.Text);
                //textboxes end

            }

        }

        private void saveSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {

            //application data folder code
            createAppDataFolder();

            if ((File.Exists(appDataStamperFolder + "\\" + "PDF Stamper.cfg")) == true)
            {
                string message = "Are you sure you want to overwrite your previous settings?";
                string caption = "Save Settings?";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result;

                // Displays the MessageBox.

                result = MessageBox.Show(message, caption, buttons);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    writeToConfigFile();
                }

                if (result == System.Windows.Forms.DialogResult.No)
                {
                    //do nothing
                }
            }

            else
            {
                //if file does not exist just create it
                writeToConfigFile();

            }

        }

        private void loadSettings()
        {

            try
            {

                int counter = 0;
                string line;

                // Read the file and display it line by line.
                System.IO.StreamReader file =
                   new System.IO.StreamReader(appDataStamperFolder + "\\" + "PDF Stamper.cfg");
                while ((line = file.ReadLine()) != null)
                {

                    if (line.Contains("radioAlignLeft checked"))
                    {
                        radioAlignLeft.Checked = true;
                    }
                    if (line.Contains("radioAlignCenter checked"))
                    {
                        radioAlignCenter.Checked = true;
                    }
                    if (line.Contains("radioAlignRight checked"))
                    {
                        radioAlignRight.Checked = true;
                    }
                    if (line.Contains("radioRed checked"))
                    {
                        radioRed.Checked = true;
                    }
                    if (line.Contains("radioBlack checked"))
                    {
                        radioBlack.Checked = true;
                    }
                    if (line.Contains("radioBlue checked"))
                    {
                        radioBlue.Checked = true;
                    }
                    if (line.Contains("radioGreen checked"))
                    {
                        radioGreen.Checked = true;
                    }
                    if (line.Contains("radioWhite checked"))
                    {
                        radioWhite.Checked = true;
                    }
                    if (line.Contains("gravityTopRadio checked"))
                    {
                        gravityTopRadio.Checked = true;
                    }
                    if (line.Contains("gravityBottomRadio checked"))
                    {
                        gravityBottomRadio.Checked = true;
                    }
                    if (line.Contains("checkBoxAutoSizeFont Checked"))
                    {
                        
                        checkBoxAutoSizeFont.Checked = true;
                    }
                    if (line.Contains("checkBoxAutoSizeFont UnChecked"))
                    {
                        
                        checkBoxAutoSizeFont.Checked = false;
                    }

                    if (line.Contains("radioButtonAllPages"))
                    {
                        radioButtonAllPages.Checked = true;
                    }
                    if (line.Contains("radioButtonOddPages"))
                    {
                        radioButtonOddPages.Checked = true;
                    }
                    if (line.Contains("radioButtonEvenPages"))
                    {
                        radioButtonEvenPages.Checked = true;
                    }
                    if (line.Contains("radioButtonFirstPage"))
                    {
                        radioButtonFirstPage.Checked = true;
                    }
                    if (line.Contains("radioButtonLastPage"))
                    {
                        radioButtonLastPage.Checked = true;
                    }

                    //regular check boxes start
                    if (line.Contains("watermarkBox checked"))
                    {
                        watermarkBox.Checked = true;
                    }
                    if (line.Contains("checkBoxFilePath checked"))
                    {
                        checkBoxFilePath.Checked = true;
                    }
                    if (line.Contains("checkBoxCustom checked"))
                    {
                        checkBoxCustom.Checked = true;
                    }
                    if (line.Contains("stampImage checked"))
                    {
                        stampImage.Checked = true;
                    }
                    //numeric controls start
                    if (line.Contains("numericFontSize"))
                    {
                        //read value after last space
                        var result = line.Substring(line.IndexOf(' ') + 1);
                        numericFontSize.Text = result;
                        result = "";
                    }

                    if (line.Contains("numericUpDownAutoSizeFont"))
                    {
                        //read value after last space
                        var result = line.Substring(line.IndexOf(' ') + 1);
                        numericUpDownAutoSizeFont.Text = result;
                        result = "";
                    }


                    if (line.Contains("numericXcoordinates"))
                    {
                        //read value after last space
                        var result = line.Substring(line.IndexOf(' ') + 1);
                        numericXcoordinates.Text = result;
                        result = "";
                    }
                    if (line.Contains("numericYcoordinates"))
                    {
                        //read value after last space
                        var result = line.Substring(line.IndexOf(' ') + 1);
                        numericYcoordinates.Text = result;
                        result = "";
                    }
                    if (line.Contains("imageXcoordinates"))
                    {
                        //read value after last space
                        var result = line.Substring(line.IndexOf(' ') + 1);
                        imageXcoordinates.Text = result;
                        result = "";
                    }
                    if (line.Contains("imageYcoordinates"))
                    {
                        //read value after last space
                        var result = line.Substring(line.IndexOf(' ') + 1);
                        imageYcoordinates.Text = result;
                        result = "";
                    }
                    if (line.Contains("imageSizePercentage"))
                    {
                        //read value after last space
                        var result = line.Substring(line.IndexOf(' ') + 1);
                        imageSizePercentage.Text = result;
                        result = "";
                    }
                    //textboxes start

                    if (line.Contains("prefixBox"))
                    {
                        //read value after last space
                        var result = line.Substring(line.IndexOf(' ') + 1);
                        prefixBox.Text = result;
                        result = "";
                    }
                    if (line.Contains("suffixBox"))
                    {
                        //read value after last space
                        var result = line.Substring(line.IndexOf(' ') + 1);
                        suffixBox.Text = result;
                        result = "";
                    }
                    if (line.Contains("textBoxCustom"))
                    {
                        //read value after last space
                        var result = line.Substring(line.IndexOf(' ') + 1);
                        textBoxCustom.Text = result;
                        result = "";
                    }
                    if (line.Contains("watermarkTextbox"))
                    {
                        //read value after last space
                        var result = line.Substring(line.IndexOf(' ') + 1);
                        watermarkTextbox.Text = result;
                        result = "";
                    }
                    if (line.Contains("imageBrowseText"))
                    {
                        //read value after last space
                        var result = line.Substring(line.IndexOf(' ') + 1);
                        imageBrowseText.Text = result;
                        result = "";
                    }
                    try
                    {
                        if (line.Contains("comboBoxFont"))
                        {
                            //read value after last space
                            var result = line.Substring(line.IndexOf(' ') + 1);
                            comboBoxFont.Text = result;
                            result = "";
                        }
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Error loading font, does font still exist?  Try saving a different font preset.");
                    }
                    if (line.Contains("checkBoxPageXofY checked"))
                    {
                        checkBoxPageXofY.Checked = true;
                    }
                    if (line.Contains("checkBoxPageNumbers checked"))
                    {
                        checkBoxPageNumbers.Checked = true;
                    }

                    counter++;
                }

                file.Close();


                if (((File.Exists(imageBrowseText.Text)) == false) && (imageBrowseText.Text != ""))
                {
                    MessageBox.Show("Your default settings will be loaded, however the image file path you previously chose no longer exists.  Image Stamp mode will be disabled unless a new image file is chosen.  The file path that no longer exists is: " + imageBrowseText.Text);
                    imageBrowseText.Text = "";
                    stampImage.Checked = false;
                }

            }

            catch (Exception)
            {
                if (File.Exists(appDataStamperFolder + "\\PDF Stamper.cfg"))
                {
                    MessageBox.Show("Unable to load custom user settings", "Settings Wizard");
                }
            }

        }

        private void loadYourSavedSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadSettings();

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void checkNowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            {
                versionLabel.Visible = true;

                try
                {
                    Application.DoEvents();

                    WebRequest request = WebRequest.Create("http://saintjohnny.com/pdffilenamelabeler/files/versionCheck.html");
                    WebResponse response = request.GetResponse();
                    System.IO.StreamReader reader = new System.IO.StreamReader(response.GetResponseStream());
                    var versionCheck = reader.ReadToEnd();

                    System.Version onlineVersion = new System.Version(versionCheck);
                    System.Version thisVersion = new System.Version(labelVersion.Text);

                    if (thisVersion < onlineVersion)
                    {
                        MessageBox.Show("Version " + onlineVersion + " available at www.saintjohnny.com", "New Version Available!");
                        versionLabel.Visible = false;
                    }

                    else if (thisVersion >= onlineVersion)
                    {
                        MessageBox.Show("You are running the current version! :)", "Nice job");
                        versionLabel.Visible = false;
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Website not available " + ex.ToString());
                    versionLabel.Visible = false;
                }

            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void howDoIStampMultiplePDFFilesInMultipleDirectoriesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("If you have multiple PDFs in sub directories, you can use windows search function, " +
                "and search for *.pdf, and then drag and drop all the search results at once into the program, and it " +
                "will stamp all of them, and to stay organized it will create the stamped folders local to each PDF file.  " +
                "Check out www.saintjohnny.com and click my youtube video links to see help videos.", "Help");
        }

        private void refreshPreview_Click(object sender, EventArgs e)
        {


            if (filesProcessedLabel.Visible == false)
            {

            }

            else
            {
                stampProcess();

                this.axAcroPDF1.src = (dragDropPathwithoutPDF + @"\Labeled PDFs\" + actualNameBeforeRemovingPDF);

                filesProcessedCountLabel.Text = "0";

                this.axAcroPDF1.setShowToolbar(false);
                this.axAcroPDF1.setPageMode("False");
                this.axAcroPDF1.setView("fit");


            }
        }

        private void checkBoxPageNumbers_CheckedChanged(object sender, EventArgs e)
        {
            {
                if (checkBoxPageNumbers.Checked == true)
                {

                    checkBoxFilePath.Checked = false;
                    prefixBox.Enabled = false;
                    suffixBox.Enabled = false;
                    watermarkBox.Enabled = false;
                    watermarkTextbox.Enabled = false;
                    checkBoxFilePath.Enabled = false;
                    checkBoxPageXofY.Checked = false;
                    checkBoxPageXofY.Enabled = false;
                    checkBoxCustom.Checked = false;
                    checkBoxCustom.Enabled = false;

                }

                else if (checkBoxPageNumbers.Checked == false)
                {
                    textBoxCustom.Enabled = false;
                    checkBoxCustom.Enabled = true;
                    prefixBox.Enabled = true;
                    suffixBox.Enabled = true;
                    watermarkBox.Enabled = true;
                    checkBoxFilePath.Enabled = true;
                    checkBoxPageXofY.Enabled = true;
                    textBoxCustom.BackColor = SystemColors.Window;
                }
            }
        }

        private void checkBoxPageXofY_CheckedChanged(object sender, EventArgs e)
        {
            {
                if (checkBoxPageXofY.Checked == true)
                {

                    checkBoxFilePath.Checked = false;

                    prefixBox.Enabled = false;
                    suffixBox.Enabled = false;
                    watermarkBox.Enabled = false;
                    watermarkTextbox.Enabled = false;
                    checkBoxFilePath.Enabled = false;
                    checkBoxPageNumbers.Checked = false;
                    checkBoxPageNumbers.Enabled = false;
                    checkBoxCustom.Checked = false;
                    checkBoxCustom.Enabled = false;

                }

                else if (checkBoxPageXofY.Checked == false)
                {
                    textBoxCustom.Enabled = false;

                    prefixBox.Enabled = true;
                    suffixBox.Enabled = true;
                    watermarkBox.Enabled = true;
                    checkBoxFilePath.Enabled = true;
                    checkBoxPageNumbers.Enabled = true;
                    textBoxCustom.BackColor = SystemColors.Window;
                    checkBoxCustom.Enabled = true;
                }
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }
        string comboFont;
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboFont = comboBoxFont.Text;

            fontSelection = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), comboFont);

        }

        private void howDoIGetSpecialCharactersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not all fonts allow special characters or international characters etc.  Check the font selection dropdown to see if you have Arial Unicode font installed or if you dont have that font you can google it and download it without having to install microsoft word.  Once downloaded, unzip it and to install it right click on the font file and selected 'install for all users' and close/re-open the PDF stamper and select that font.", "Help");
        }

        private void numericXcoordinates_ValueChanged(object sender, EventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (numericXcoordinates.Text.Contains("-"))
            {
                numericXcoordinates.Text = "3";
                MessageBox.Show("Negative values not allowed, X has been reset to 3");
            }
            if (numericYcoordinates.Text.Contains("-"))
            {
                numericYcoordinates.Text = "3";
                MessageBox.Show("Negative values not allowed, Y has been reset to 3");
            }

            if (checkBoxAutoSizeFont.Checked == true)
            {
                numericUpDownAutoSizeFont.Enabled = true;
                numericFontSize.Enabled = false;
            }
            else
            {
                numericUpDownAutoSizeFont.Enabled = false;
                numericFontSize.Enabled = true;
            }

            if (numericUpDownAutoSizeFont.Text.Contains("-"))
            {
                numericUpDownAutoSizeFont.Text = "120";
                MessageBox.Show("Negative values not allowed, Y has been reset to 120");
            }



        }

        private void howDoIStampMultipleThingsAtTheSameTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("You can only stamp one thing at a time for the most part, however for example you could always stamp one thing, then go into the stamped folder and then stamp the already stamped file with your second stamp as many times as you like.", "Help");
        }

        private void checkBoxAutoSizeFont_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDownAutoSizeFont_Leave(object sender, EventArgs e)
        {
            if (Convert.ToInt32(numericUpDownAutoSizeFont.Text) < 40)
            {
                numericUpDownAutoSizeFont.Text = "40";
                MessageBox.Show("auto font size is too low, it has been changed to 40 which the minimum, otherwise errors may occur.");
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Created by John Hubert.  If you are in the DAS industry reach out to me!","About");
        }
    }
}

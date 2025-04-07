using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
using TextFSM;

namespace TextFSMTester
{
    public partial class TemplateEngineTester : MaterialForm
    {
        private readonly MaterialSkinManager materialSkinManager;
        private enum OutputFormat
        {
            Default,
            JSON
        }

        public TemplateEngineTester()
        {
            // Initialize MaterialSkinManager
            materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.EnforceBackcolorOnAllComponents = true;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.BlueGrey800, 
                Primary.BlueGrey900, 
                Primary.BlueGrey500, 
                Accent.LightBlue200, 
                TextShade.WHITE);

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "TextFSM Template Engine Tester";
            
            // Set the form size to 80% of the screen
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            this.Size = new Size((int)(screen.Width * 0.8), (int)(screen.Height * 0.8));
            this.MinimumSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Set a more compact default font for the whole form
            this.Font = new Font("Segoe UI", 9F);
            
            // Create a MaterialTabControl for organizing the interface
            MaterialTabControl tabControl = new MaterialTabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Multiline = true;

            // Create tabs
            TabPage mainTab = new TabPage("Template Tester");
            mainTab.BackColor = materialSkinManager.BackgroundColor;

            // Add tabs to control
            tabControl.TabPages.Add(mainTab);

            // Create SplitContainer for a horizontal splitter
            SplitContainer mainSplitContainer = new SplitContainer();
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.Orientation = Orientation.Horizontal;
            mainSplitContainer.SplitterDistance = (int)(this.Height * 0.65);
            mainSplitContainer.SplitterWidth = 4;
            mainSplitContainer.Panel1MinSize = 200;
            mainSplitContainer.Panel2MinSize = 150;
            mainSplitContainer.BackColor = materialSkinManager.BackgroundColor;

            // Top panel - Source and Result
            TableLayoutPanel topPanel = new TableLayoutPanel();
            topPanel.Dock = DockStyle.Fill;
            topPanel.ColumnCount = 2;
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            topPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            topPanel.BackColor = materialSkinManager.BackgroundColor;

            // Bottom panel - Template and buttons
            TableLayoutPanel bottomPanel = new TableLayoutPanel();
            bottomPanel.Dock = DockStyle.Fill;
            bottomPanel.ColumnCount = 2;
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F)); // Template gets 70%
            bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); // Buttons get 30%
            bottomPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            bottomPanel.BackColor = materialSkinManager.BackgroundColor;
            
            // Create a compact font for labels and textboxes
            Font compactLabelFont = new Font("Segoe UI", 9F);
            Font compactTextFont = new Font("Consolas", 8.5F);
            
            // Source panel
            Panel sourcePanel = new Panel();
            sourcePanel.Dock = DockStyle.Fill;
            sourcePanel.Padding = new Padding(4);
            sourcePanel.BackColor = materialSkinManager.BackgroundColor;
            sourcePanel.Margin = new Padding(0);

            MaterialLabel sourceLabel = new MaterialLabel();
            sourceLabel.Text = "Source";
            sourceLabel.Dock = DockStyle.Top;
            sourceLabel.Height = 24;
            sourceLabel.Font = compactLabelFont;

            MaterialMultiLineTextBox2 sourceTextBox = new MaterialMultiLineTextBox2();
            sourceTextBox.Dock = DockStyle.Fill;
            sourceTextBox.BackColor = materialSkinManager.BackgroundColor;
            sourceTextBox.Font = compactTextFont;
            sourceTextBox.Padding = new Padding(2);
            sourceTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;

            sourceTextBox.Text = "Interface                  IP-Address      OK? Method Status                Protocol\r\n" +
                   "FastEthernet0/0            192.168.1.1     YES NVRAM  up                    up\r\n" +
                   "FastEthernet0/1            unassigned      YES NVRAM  administratively down down\r\n" +
                   "FastEthernet0/2            192.168.2.1     YES NVRAM  up                    up";

            sourcePanel.Controls.Add(sourceTextBox);
            sourcePanel.Controls.Add(sourceLabel);

            // Result panel
            Panel resultPanel = new Panel();
            resultPanel.Dock = DockStyle.Fill;
            resultPanel.Padding = new Padding(4);
            resultPanel.BackColor = materialSkinManager.BackgroundColor;
            resultPanel.Margin = new Padding(0);

            MaterialLabel resultLabel = new MaterialLabel();
            resultLabel.Text = "Result";
            resultLabel.Dock = DockStyle.Top;
            resultLabel.Height = 24;
            resultLabel.Font = compactLabelFont;

            MaterialMultiLineTextBox2 resultTextBox = new MaterialMultiLineTextBox2();
            resultTextBox.Dock = DockStyle.Fill;
            resultTextBox.ReadOnly = true;
            resultTextBox.Font = compactTextFont;
            resultTextBox.Padding = new Padding(2);
            resultTextBox.BackColor = materialSkinManager.BackgroundColor;
            resultTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;


            resultPanel.Controls.Add(resultTextBox);
            resultPanel.Controls.Add(resultLabel);

            // Template panel
            Panel templatePanel = new Panel();
            templatePanel.Dock = DockStyle.Fill;
            templatePanel.Padding = new Padding(4);
            templatePanel.BackColor = materialSkinManager.BackgroundColor;
            templatePanel.Margin = new Padding(0);

            MaterialLabel templateLabel = new MaterialLabel();
            templateLabel.Text = "Template";
            templateLabel.Dock = DockStyle.Top;
            templateLabel.Height = 24;
            templateLabel.Font = compactLabelFont;

            MaterialMultiLineTextBox2 templateTextBox = new MaterialMultiLineTextBox2();
            templateTextBox.Dock = DockStyle.Fill;
            templateTextBox.BackColor = materialSkinManager.BackgroundColor;
            templateTextBox.Font = compactTextFont;
            templateTextBox.Padding = new Padding(2);
            templateTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            templateTextBox.Text = "Value Interface (\\S+)\r\n" +
                   "Value IP_Address (\\S+)\r\n" +
                   "Value OK (\\S+)\r\n" +
                   "Value Method (\\S+)\r\n" +
                   "Value Status (.+?)\r\n" +
                   "Value Protocol (\\S+)\r\n" +
                   "\r\n" +
                   "Start\r\n" +
                   "  ^${Interface}\\s+${IP_Address}\\s+${OK}\\s+${Method}\\s+${Status}\\s+${Protocol}\\s*$ -> Record";

            templatePanel.Controls.Add(templateTextBox);
            templatePanel.Controls.Add(templateLabel);
            
            // Button panel with vertical layout
            Panel buttonPanel = new Panel();
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.Padding = new Padding(2);
            buttonPanel.BackColor = materialSkinManager.BackgroundColor;
            buttonPanel.Margin = new Padding(0);
            
            // Create button container with vertical flow layout
            TableLayoutPanel buttonContainer = new TableLayoutPanel();
            buttonContainer.Dock = DockStyle.Fill;
            buttonContainer.RowCount = 2;
            buttonContainer.ColumnCount = 1;
            buttonContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Buttons row
            buttonContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // ComboBox row
            buttonContainer.Padding = new Padding(4);
            
            // Flow panel for buttons (top part)
            FlowLayoutPanel buttonFlowPanel = new FlowLayoutPanel();
            buttonFlowPanel.Dock = DockStyle.Fill;
            buttonFlowPanel.FlowDirection = FlowDirection.LeftToRight;
            buttonFlowPanel.WrapContents = false;
            buttonFlowPanel.AutoScroll = true;
            buttonFlowPanel.Padding = new Padding(0);
            
            // Panel for ComboBox (bottom part)
            FlowLayoutPanel comboBoxPanel = new FlowLayoutPanel();
            comboBoxPanel.Dock = DockStyle.Fill;
            comboBoxPanel.FlowDirection = FlowDirection.LeftToRight;
            comboBoxPanel.WrapContents = false;
            comboBoxPanel.AutoScroll = true;
            comboBoxPanel.Padding = new Padding(0);
            
            // Custom font for compact UI
            Font compactFont = new Font("Segoe UI", 8F);
            // Smaller font for ComboBox items
            Font smallerFont = new Font("Segoe UI", 7F);

            MaterialButton renderButton = new MaterialButton();
            renderButton.Text = "RENDER";
            renderButton.AutoSize = false;
            renderButton.Width = 90;
            renderButton.Height = 28;
            renderButton.Margin = new Padding(2);
            renderButton.UseAccentColor = true;
            renderButton.Font = compactFont;
            renderButton.Click += (sender, e) => RenderTemplate(templateTextBox.Text, sourceTextBox.Text, resultTextBox);

            MaterialButton clearButton = new MaterialButton();
            clearButton.Text = "CLEAR";
            clearButton.AutoSize = false;
            clearButton.Width = 90;
            clearButton.Height = 28;
            clearButton.Margin = new Padding(2);
            clearButton.Font = compactFont;
            clearButton.Click += (sender, e) => ClearAll(templateTextBox, sourceTextBox, resultTextBox);

            MaterialButton exampleButton = new MaterialButton();
            exampleButton.Text = "EXAMPLE";
            exampleButton.AutoSize = false;
            exampleButton.Width = 90;
            exampleButton.Height = 28;
            exampleButton.Margin = new Padding(2);
            exampleButton.Font = compactFont;
            exampleButton.Click += (sender, e) => LoadExample(templateTextBox, sourceTextBox);

            // Standard ComboBox with custom styling to match Material design
            MaterialLabel formatLabel = new MaterialLabel();
            formatLabel.Text = "Format:";
            formatLabel.AutoSize = true;
            formatLabel.Margin = new Padding(2);
            formatLabel.Font = compactFont;
            
            // Using a standard ComboBox for better customization
            ComboBox formatComboBox = new ComboBox();
            formatComboBox.Name = "formatComboBox";
            formatComboBox.Items.Add("Default Format");
            formatComboBox.Items.Add("JSON");
            formatComboBox.SelectedIndex = 0; // Default selection
            formatComboBox.Size = new Size(150, 24);
            formatComboBox.Margin = new Padding(2);
            formatComboBox.Font = smallerFont;
            formatComboBox.BackColor = materialSkinManager.BackgroundColor;
            formatComboBox.ForeColor = Color.White;
            formatComboBox.FlatStyle = FlatStyle.Flat;
            formatComboBox.ItemHeight = 16;
            formatComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            
            // Add buttons to the button flow panel
            buttonFlowPanel.Controls.Add(renderButton);
            buttonFlowPanel.Controls.Add(clearButton);
            buttonFlowPanel.Controls.Add(exampleButton);
            
            // Add ComboBox to the panel
            comboBoxPanel.Controls.Add(formatLabel);
            comboBoxPanel.Controls.Add(formatComboBox);
            
            // Add panels to the button container
            buttonContainer.Controls.Add(buttonFlowPanel, 0, 0);
            buttonContainer.Controls.Add(comboBoxPanel, 0, 1);
            
            // Add button container to the button panel
            buttonPanel.Controls.Add(buttonContainer);

            // Add panels to the top layout
            topPanel.Controls.Add(sourcePanel, 0, 0);
            topPanel.Controls.Add(resultPanel, 1, 0);

            // Add panels to the bottom layout
            bottomPanel.Controls.Add(templatePanel, 0, 0);
            bottomPanel.Controls.Add(buttonPanel, 1, 0);

            // Add layouts to the split container
            mainSplitContainer.Panel1.Controls.Add(topPanel);
            mainSplitContainer.Panel2.Controls.Add(bottomPanel);

            // Add split container to the tab
            mainTab.Controls.Add(mainSplitContainer);

            // Add tab control to form
            this.Controls.Add(tabControl);

            // Add Material tab selector
            MaterialTabSelector tabSelector = new MaterialTabSelector();
            tabSelector.Dock = DockStyle.Top;
            tabSelector.BaseTabControl = tabControl;
            this.Controls.Add(tabSelector);

            this.ResumeLayout(false);
        }

        private void RenderTemplate(string template, string source, MaterialMultiLineTextBox2 resultTextBox)
        {
            try
            {
                resultTextBox.Clear();
                
                // Create TextFSM instance
                var fsm = new TextFSM.TextFSM(template);
                
                // Parse data
                var results = fsm.ParseText(source);
                
                // Determine output format based on combo box selection
                OutputFormat format = OutputFormat.Default;
                var formatComboBox = Controls.Find("formatComboBox", true).FirstOrDefault() as ComboBox;
                if (formatComboBox != null && formatComboBox.SelectedIndex == 1) // JSON is index 1
                {
                    format = OutputFormat.JSON;
                }
                
                // Display results based on format
                if (format == OutputFormat.JSON)
                {
                    DisplayResultsAsJson(fsm.Header, results, resultTextBox);
                }
                else
                {
                    DisplayResultsDefault(fsm.Header, results, resultTextBox);
                }
            }
            catch (Exception ex)
            {
                resultTextBox.Text = $"Error: {ex.Message}\r\n\r\n{ex.StackTrace}";
            }
        }

        private void DisplayResultsDefault(IEnumerable<string> header, List<List<object>> results, MaterialMultiLineTextBox2 resultTextBox)
        {
            // Clear first
            resultTextBox.Text = "";
            
            // Build the entire output string
            string output = $"Header: [";
            output += string.Join(", ", header.Select(h => $"'{h}'").ToArray());
            output += "]\r\n\r\n";
            
            output += "Data:\r\n";
            
            // Print header row
            output += "[";
            output += string.Join(", ", header.Select(h => $"'{h}'").ToArray());
            output += "]\r\n";
            
            // Print data rows
            foreach (var record in results)
            {
                output += "[";
                output += string.Join(", ", record.Select(r => $"'{r}'").ToArray());
                output += "]\r\n";
            }
            
            // Set the text in one operation
            resultTextBox.Text = output;
        }

        private void DisplayResultsAsJson(IEnumerable<string> header, List<List<object>> results, MaterialMultiLineTextBox2 resultTextBox)
        {
            // Convert to JSON format
            var jsonList = new List<Dictionary<string, string>>();
            
            foreach (var record in results)
            {
                var jsonRecord = new Dictionary<string, string>();
                var headerArray = header.ToArray();
                
                for (int i = 0; i < headerArray.Length; i++)
                {
                    if (i < record.Count)
                    {
                        jsonRecord[headerArray[i]] = record[i]?.ToString() ?? string.Empty;
                    }
                }
                
                jsonList.Add(jsonRecord);
            }
            
            // Convert to JSON string with proper formatting
            var jsonOptions = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            string jsonString = System.Text.Json.JsonSerializer.Serialize(jsonList, jsonOptions);
            
            // Set the text directly
            resultTextBox.Text = jsonString;
        }

        private void ClearAll(MaterialMultiLineTextBox2 templateTextBox, MaterialMultiLineTextBox2 sourceTextBox, MaterialMultiLineTextBox2 resultTextBox)
        {
            templateTextBox.Clear();
            sourceTextBox.Clear();
            resultTextBox.Clear();
        }

        private void LoadExample(MaterialMultiLineTextBox2 templateTextBox, MaterialMultiLineTextBox2 sourceTextBox)
        {
            templateTextBox.Text = "Value Interface (\\S+)\r\n" +
                       "Value IP_Address (\\S+)\r\n" +
                       "Value OK (\\S+)\r\n" +
                       "Value Method (\\S+)\r\n" +
                       "Value Status (.+?)\r\n" +
                       "Value Protocol (\\S+)\r\n" +
                       "\r\n" +
                       "Start\r\n" +
                       "  ^${Interface}\\s+${IP_Address}\\s+${OK}\\s+${Method}\\s+${Status}\\s+${Protocol}\\s*$ -> Record";
                       
            sourceTextBox.Text = "Interface                  IP-Address      OK? Method Status                Protocol\r\n" +
                       "FastEthernet0/0            192.168.1.1     YES NVRAM  up                    up\r\n" +
                       "FastEthernet0/1            unassigned      YES NVRAM  administratively down down\r\n" +
                       "FastEthernet0/2            192.168.2.1     YES NVRAM  up                    up";
        }
    }
}
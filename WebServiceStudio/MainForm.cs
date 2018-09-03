using System.Security.Cryptography.X509Certificates;

namespace WebServiceStudio
{
  using System;
  using System.ComponentModel;
  using System.Data;
  using System.Drawing;
  using System.IO;
  using System.Net;
  using System.Reflection;
  using System.Runtime.CompilerServices;
  using System.Text;
  using System.Threading;
  using System.Web.Services.Protocols;
  using System.Windows.Forms;
  using System.Xml.Serialization;

  public class MainForm : Form
  {
    private Button buttonBrowseFile;
    private Button buttonGet;
    private Button buttonInvoke;
    private Button buttonSend;
    private IContainer components;
    private RichTextBoxFinds findOption = RichTextBoxFinds.None;
    private static bool isV1 = false;
    private Label labelEndPointUrl;
    private Label labelInput;
    private Label labelInputValue;
    private Label labelOutput;
    private Label labelOutputValue;
    private Label labelRequest;
    private Label labelResponse;
    private static MainForm mainForm;
    private MainMenu mainMenu1;
    private MenuItem menuItem1;
    private MenuItem menuItem2;
    private MenuItem menuItem3;
    private MenuItem menuItemAbout;
    private MenuItem menuItemExit;
    private MenuItem menuItemFind;
    private MenuItem menuItemFindNext;
    private MenuItem menuItemHelp;
    private MenuItem menuItemOptions;
    private MenuItem menuItemSaveAll;
    private MenuItem menuItemTreeInputCopy;
    private MenuItem menuItemTreeInputPaste;
    private MenuItem menuItemTreeOutputCopy;
    private static string MiniHelpText = "\r\n        .NET Webservice Studio is a tool to invoke webmethods interactively. The user can provide a WSDL endpoint. On clicking button Get the tool fetches the WSDL, generates .NET proxy from the WSDL and displays the list of methods available. The user can choose any method and provide the required input parameters. On clicking Invoke the SOAP request is sent to the server and the response is parsed to display the return value.\r\n        ";
    private OpenFileDialog openWsdlDialog;
    private Panel panelBottomMain;
    private Panel panelLeftInvoke;
    private Panel panelLeftRaw;
    private Panel panelLeftWsdl;
    private Panel panelRightInvoke;
    private Panel panelRightRaw;
    private Panel panelRightWsdl;
    private Panel panelTopMain;
    private PropertyGrid propInput;
    private PropertyGrid propOutput;
    private PropertyGrid propRequest;
    private RichTextBox richMessage;
    private RichTextBox richRequest;
    private RichTextBox richResponse;
    private RichTextBox richWsdl;
    private SaveFileDialog saveAllDialog;
    private string searchStr = "";
    private Splitter splitterInvoke;
    private Splitter splitterRaw;
    private Splitter splitterWsdl;
    private TabControl tabMain;
    private TabPage tabPageInvoke;
    private TabPage tabPageMessage;
    private TabPage tabPageRaw;
    private TabPage tabPageWsdl;
    private ComboBox textEndPointUri;
    private ToolBarButton toolBarButton1;
    private TreeView treeInput;
    private TreeView treeMethods;
    private TreeView treeOutput;
    private TreeView treeWsdl;
    private TextBox txtCertName;
    private Label label1;
    private WebServiceStudio.Wsdl wsdl = null;

    public MainForm()
    {
      this.InitializeComponent();
      this.wsdl = new WebServiceStudio.Wsdl();
      Control.CheckForIllegalCrossThreadCalls = false;

      #region Initaliazliation
      this.richWsdl.Font = Configuration.MasterConfig.UiSettings.WsdlFont;
      this.textEndPointUri.Items.AddRange(Configuration.MasterConfig.InvokeSettings.RecentlyUsedUris);
      this.richMessage.Font = Configuration.MasterConfig.UiSettings.MessageFont;
      this.richRequest.Font = Configuration.MasterConfig.UiSettings.ReqRespFont;
      this.richResponse.Font = Configuration.MasterConfig.UiSettings.ReqRespFont;
      this.txtCertName.Text = Configuration.MasterConfig.InvokeSettings.CertName;

      #endregion

    }

    private void buttonBrowseFile_Click(object sender, EventArgs e)
    {
      if (this.openWsdlDialog.ShowDialog() == DialogResult.OK)
      {
        this.textEndPointUri.Text = this.openWsdlDialog.FileName;
      }
    }

    private void buttonGet_Click(object sender, EventArgs e)
    {
      if (this.buttonGet.Text == "Get")
      {
        this.ClearAllTabs();
        TabPage selectedTab = this.tabMain.SelectedTab;
        this.tabMain.SelectedTab = this.tabPageMessage;
        string text = this.textEndPointUri.Text;
        this.wsdl.Reset();
        this.wsdl.Paths.Add(text);
        this.wsdl.CertName = txtCertName.Text;
        new Thread(new ThreadStart(this.wsdl.Generate)).Start();
        this.buttonGet.Text = "Cancel";
      }
      else
      {
        this.buttonGet.Text = "Get";
        this.ShowMessageInternal(this, MessageType.Failure, "Cancelled");
        this.wsdl.Reset();
        this.wsdl = new WebServiceStudio.Wsdl();
      }
    }

    private void buttonInvoke_Click(object sender, EventArgs e)
    {
      Cursor cursor = this.Cursor;
      this.Cursor = Cursors.WaitCursor;
      try
      {
        this.propOutput.SelectedObject = null;
        this.treeOutput.Nodes.Clear();
        this.InvokeWebMethod();
      }
      finally
      {
        this.Cursor = cursor;
      }
    }

    private void buttonSend_Click(object sender, EventArgs e)
    {
      this.SendWebRequest();
    }

    private void ClearAllTabs()
    {
      this.richWsdl.Clear();
      this.richWsdl.Font = Configuration.MasterConfig.UiSettings.WsdlFont;
      this.treeWsdl.Nodes.Clear();
      this.richMessage.Clear();
      this.richMessage.Font = Configuration.MasterConfig.UiSettings.MessageFont;
      this.richRequest.Clear();
      this.richRequest.Font = Configuration.MasterConfig.UiSettings.ReqRespFont;
      this.richResponse.Clear();
      this.richResponse.Font = Configuration.MasterConfig.UiSettings.ReqRespFont;
      this.treeMethods.Nodes.Clear();
      TreeNodeProperty.ClearIncludedTypes();
      this.treeInput.Nodes.Clear();
      this.treeOutput.Nodes.Clear();
      this.propInput.SelectedObject = null;
      this.propOutput.SelectedObject = null;
    }

    private void CopyToClipboard(TreeNodeProperty tnp)
    {
      if (!this.IsValidCopyNode(tnp))
      {
        throw new Exception("Cannot copy from here");
      }
      object o = tnp.ReadChildren();
      if (o != null)
      {
        StringWriter writer = new StringWriter();
        System.Type[] extraTypes = new System.Type[] { o.GetType() };
        System.Type type = (o is DataSet) ? typeof(DataSet) : typeof(object);
        new XmlSerializer(type, extraTypes).Serialize((TextWriter)writer, o);
        Clipboard.SetDataObject(writer.ToString());
      }
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && (this.components != null))
      {
        this.components.Dispose();
      }
      base.Dispose(disposing);
    }

    private void DumpResponse(HttpWebResponse response)
    {
      this.richResponse.Text = WSSWebResponse.DumpResponse(response);
    }

    private void FillInvokeTab()
    {
      Assembly proxyAssembly = this.wsdl.ProxyAssembly;
      if (proxyAssembly != null)
      {
        this.treeMethods.Nodes.Clear();
        foreach (System.Type type in proxyAssembly.GetTypes())
        {
          if (TreeNodeProperty.IsWebService(type))
          {
            TreeNode node = this.treeMethods.Nodes.Add(type.Name);
            HttpWebClientProtocol proxy = (HttpWebClientProtocol)Activator.CreateInstance(type);
            ProxyProperty property = new ProxyProperty(proxy);
            property.RecreateSubtree(null);
            node.Tag = property.TreeNode;
            proxy.Credentials = CredentialCache.DefaultCredentials;
            SoapHttpClientProtocol protocol2 = proxy as SoapHttpClientProtocol;
            if (protocol2 != null)
            {
              protocol2.CookieContainer = new CookieContainer();
              protocol2.AllowAutoRedirect = true;
            }
            foreach (MethodInfo info in type.GetMethods())
            {
              if (TreeNodeProperty.IsWebMethod(info))
              {
                node.Nodes.Add(info.Name).Tag = info;
              }
            }
          }
        }
        this.treeMethods.ExpandAll();
      }
    }

    private void FillWsdlTab()
    {
      if ((this.wsdl.Wsdls != null) && (this.wsdl.Wsdls.Count != 0))
      {
        int num3;
        this.richWsdl.Text = this.wsdl.Wsdls[0];
        this.treeWsdl.Nodes.Clear();
        TreeNode node = this.treeWsdl.Nodes.Add("WSDLs");
        XmlTreeWriter writer = new XmlTreeWriter();
        for (int i = 0; i < this.wsdl.Wsdls.Count; i++)
        {
          num3 = i + 1;
          TreeNode root = node.Nodes.Add("WSDL#" + num3.ToString());
          root.Tag = this.wsdl.Wsdls[i];
          writer.FillTree(this.wsdl.Wsdls[i], root);
        }
        TreeNode node3 = this.treeWsdl.Nodes.Add("Schemas");
        for (int j = 0; j < this.wsdl.Xsds.Count; j++)
        {
          num3 = j + 1;
          TreeNode node4 = node3.Nodes.Add("Schema#" + num3.ToString());
          node4.Tag = this.wsdl.Xsds[j];
          writer.FillTree(this.wsdl.Xsds[j], node4);
        }
        this.treeWsdl.Nodes.Add("Proxy").Tag = this.wsdl.ProxyCode;
        this.treeWsdl.Nodes.Add("ClientCode").Tag = "Shows client code for all methods accessed in the invoke tab";
        node.Expand();
      }
    }

    private void Find()
    {
      this.tabMain.SelectedTab = this.tabPageWsdl;
      this.richWsdl.Find(this.searchStr, this.richWsdl.SelectionStart + this.richWsdl.SelectionLength, this.findOption);
    }

    private string GenerateClientCode()
    {
      Script script = new Script(this.wsdl.ProxyNamespace, "MainClass");
      foreach (TreeNode node in this.treeMethods.Nodes)
      {
        script.Proxy = this.GetProxyPropertyFromNode(node).GetProxy();
        foreach (TreeNode node2 in node.Nodes)
        {
          TreeNode tag = node2.Tag as TreeNode;
          if (tag != null)
          {
            MethodProperty property = tag.Tag as MethodProperty;
            if (property != null)
            {
              MethodInfo method = property.GetMethod();
              object[] parameters = property.ReadChildren() as object[];
              script.AddMethod(method, parameters);
            }
          }
        }
      }
      return script.Generate(this.wsdl.GetCodeGenerator());
    }

    private MethodProperty GetCurrentMethodProperty()
    {
      if ((this.treeInput.Nodes == null) || (this.treeInput.Nodes.Count == 0))
      {
        MessageBox.Show(this, "Select a web method to execute");
        return null;
      }
      TreeNode node = this.treeInput.Nodes[0];
      MethodProperty tag = node.Tag as MethodProperty;
      if (tag == null)
      {
        MessageBox.Show(this, "Select a method to execute");
        return null;
      }
      return tag;
    }

    private ProxyProperty GetProxyPropertyFromNode(TreeNode treeNode)
    {
      while (treeNode.Parent != null)
      {
        treeNode = treeNode.Parent;
      }
      TreeNode tag = treeNode.Tag as TreeNode;
      if (tag != null)
      {
        return (tag.Tag as ProxyProperty);
      }
      return null;
    }

    private void InitializeComponent()
    {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.textEndPointUri = new System.Windows.Forms.ComboBox();
            this.buttonGet = new System.Windows.Forms.Button();
            this.labelEndPointUrl = new System.Windows.Forms.Label();
            this.mainMenu1 = new System.Windows.Forms.MainMenu(this.components);
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItemSaveAll = new System.Windows.Forms.MenuItem();
            this.menuItemExit = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.menuItemFind = new System.Windows.Forms.MenuItem();
            this.menuItemFindNext = new System.Windows.Forms.MenuItem();
            this.menuItemOptions = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.menuItemHelp = new System.Windows.Forms.MenuItem();
            this.menuItemAbout = new System.Windows.Forms.MenuItem();
            this.menuItemTreeOutputCopy = new System.Windows.Forms.MenuItem();
            this.menuItemTreeInputCopy = new System.Windows.Forms.MenuItem();
            this.menuItemTreeInputPaste = new System.Windows.Forms.MenuItem();
            this.openWsdlDialog = new System.Windows.Forms.OpenFileDialog();
            this.toolBarButton1 = new System.Windows.Forms.ToolBarButton();
            this.buttonBrowseFile = new System.Windows.Forms.Button();
            this.saveAllDialog = new System.Windows.Forms.SaveFileDialog();
            this.tabPageInvoke = new System.Windows.Forms.TabPage();
            this.splitterInvoke = new System.Windows.Forms.Splitter();
            this.panelRightInvoke = new System.Windows.Forms.Panel();
            this.labelOutputValue = new System.Windows.Forms.Label();
            this.labelInputValue = new System.Windows.Forms.Label();
            this.labelOutput = new System.Windows.Forms.Label();
            this.labelInput = new System.Windows.Forms.Label();
            this.treeInput = new System.Windows.Forms.TreeView();
            this.treeOutput = new System.Windows.Forms.TreeView();
            this.propOutput = new System.Windows.Forms.PropertyGrid();
            this.propInput = new System.Windows.Forms.PropertyGrid();
            this.buttonInvoke = new System.Windows.Forms.Button();
            this.panelLeftInvoke = new System.Windows.Forms.Panel();
            this.treeMethods = new System.Windows.Forms.TreeView();
            this.tabPageWsdl = new System.Windows.Forms.TabPage();
            this.splitterWsdl = new System.Windows.Forms.Splitter();
            this.panelRightWsdl = new System.Windows.Forms.Panel();
            this.richWsdl = new System.Windows.Forms.RichTextBox();
            this.panelLeftWsdl = new System.Windows.Forms.Panel();
            this.treeWsdl = new System.Windows.Forms.TreeView();
            this.tabPageMessage = new System.Windows.Forms.TabPage();
            this.richMessage = new System.Windows.Forms.RichTextBox();
            this.tabPageRaw = new System.Windows.Forms.TabPage();
            this.splitterRaw = new System.Windows.Forms.Splitter();
            this.panelRightRaw = new System.Windows.Forms.Panel();
            this.buttonSend = new System.Windows.Forms.Button();
            this.richRequest = new System.Windows.Forms.RichTextBox();
            this.richResponse = new System.Windows.Forms.RichTextBox();
            this.labelRequest = new System.Windows.Forms.Label();
            this.labelResponse = new System.Windows.Forms.Label();
            this.panelLeftRaw = new System.Windows.Forms.Panel();
            this.propRequest = new System.Windows.Forms.PropertyGrid();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.panelTopMain = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.txtCertName = new System.Windows.Forms.TextBox();
            this.panelBottomMain = new System.Windows.Forms.Panel();
            this.tabPageInvoke.SuspendLayout();
            this.panelRightInvoke.SuspendLayout();
            this.panelLeftInvoke.SuspendLayout();
            this.tabPageWsdl.SuspendLayout();
            this.panelRightWsdl.SuspendLayout();
            this.panelLeftWsdl.SuspendLayout();
            this.tabPageMessage.SuspendLayout();
            this.tabPageRaw.SuspendLayout();
            this.panelRightRaw.SuspendLayout();
            this.panelLeftRaw.SuspendLayout();
            this.tabMain.SuspendLayout();
            this.panelTopMain.SuspendLayout();
            this.panelBottomMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // textEndPointUri
            // 
            this.textEndPointUri.Location = new System.Drawing.Point(106, 20);
            this.textEndPointUri.Name = "textEndPointUri";
            this.textEndPointUri.Size = new System.Drawing.Size(532, 20);
            this.textEndPointUri.TabIndex = 1;
            this.textEndPointUri.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textEndPointUri_KeyPress);
            // 
            // buttonGet
            // 
            this.buttonGet.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonGet.Location = new System.Drawing.Point(646, 17);
            this.buttonGet.Name = "buttonGet";
            this.buttonGet.Size = new System.Drawing.Size(72, 26);
            this.buttonGet.TabIndex = 3;
            this.buttonGet.Text = "Get";
            this.buttonGet.Click += new System.EventHandler(this.buttonGet_Click);
            // 
            // labelEndPointUrl
            // 
            this.labelEndPointUrl.Location = new System.Drawing.Point(12, 17);
            this.labelEndPointUrl.Name = "labelEndPointUrl";
            this.labelEndPointUrl.Size = new System.Drawing.Size(94, 26);
            this.labelEndPointUrl.TabIndex = 0;
            this.labelEndPointUrl.Text = "WSDL EndPoint";
            this.labelEndPointUrl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1,
            this.menuItem2,
            this.menuItem3});
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 0;
            this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemSaveAll,
            this.menuItemExit});
            this.menuItem1.Text = "File";
            // 
            // menuItemSaveAll
            // 
            this.menuItemSaveAll.Index = 0;
            this.menuItemSaveAll.Text = "Save All Files...";
            this.menuItemSaveAll.Click += new System.EventHandler(this.menuItemSaveAll_Click);
            // 
            // menuItemExit
            // 
            this.menuItemExit.Index = 1;
            this.menuItemExit.Text = "Exit";
            this.menuItemExit.Click += new System.EventHandler(this.menuItemExit_Click);
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 1;
            this.menuItem2.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemFind,
            this.menuItemFindNext,
            this.menuItemOptions});
            this.menuItem2.Text = "Edit";
            // 
            // menuItemFind
            // 
            this.menuItemFind.Index = 0;
            this.menuItemFind.Shortcut = System.Windows.Forms.Shortcut.CtrlF;
            this.menuItemFind.Text = "Find...";
            this.menuItemFind.Click += new System.EventHandler(this.menuItemFind_Click);
            // 
            // menuItemFindNext
            // 
            this.menuItemFindNext.Index = 1;
            this.menuItemFindNext.Shortcut = System.Windows.Forms.Shortcut.F3;
            this.menuItemFindNext.Text = "Find Next";
            this.menuItemFindNext.Click += new System.EventHandler(this.menuItemFindNext_Click);
            // 
            // menuItemOptions
            // 
            this.menuItemOptions.Index = 2;
            this.menuItemOptions.Text = "Options...";
            this.menuItemOptions.Click += new System.EventHandler(this.menuItemOptions_Click);
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 2;
            this.menuItem3.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemHelp,
            this.menuItemAbout});
            this.menuItem3.Text = "Help";
            // 
            // menuItemHelp
            // 
            this.menuItemHelp.Index = 0;
            this.menuItemHelp.Text = "Help";
            this.menuItemHelp.Click += new System.EventHandler(this.menuItemHelp_Click);
            // 
            // menuItemAbout
            // 
            this.menuItemAbout.Index = 1;
            this.menuItemAbout.Text = "About...";
            this.menuItemAbout.Click += new System.EventHandler(this.menuItemAbout_Click);
            // 
            // menuItemTreeOutputCopy
            // 
            this.menuItemTreeOutputCopy.Index = -1;
            this.menuItemTreeOutputCopy.Shortcut = System.Windows.Forms.Shortcut.CtrlC;
            this.menuItemTreeOutputCopy.Text = "Copy";
            this.menuItemTreeOutputCopy.Click += new System.EventHandler(this.treeOutputMenuCopy_Click);
            // 
            // menuItemTreeInputCopy
            // 
            this.menuItemTreeInputCopy.Index = -1;
            this.menuItemTreeInputCopy.Shortcut = System.Windows.Forms.Shortcut.CtrlC;
            this.menuItemTreeInputCopy.Text = "Copy";
            this.menuItemTreeInputCopy.Click += new System.EventHandler(this.treeInputMenuCopy_Click);
            // 
            // menuItemTreeInputPaste
            // 
            this.menuItemTreeInputPaste.Index = -1;
            this.menuItemTreeInputPaste.Shortcut = System.Windows.Forms.Shortcut.CtrlV;
            this.menuItemTreeInputPaste.Text = "Paste";
            this.menuItemTreeInputPaste.Click += new System.EventHandler(this.treeInputMenuPaste_Click);
            // 
            // toolBarButton1
            // 
            this.toolBarButton1.Name = "toolBarButton1";
            this.toolBarButton1.Text = "Open Wsdl...";
            this.toolBarButton1.ToolTipText = "Open WSDL file(s)";
            // 
            // buttonBrowseFile
            // 
            this.buttonBrowseFile.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonBrowseFile.Location = new System.Drawing.Point(730, 17);
            this.buttonBrowseFile.Name = "buttonBrowseFile";
            this.buttonBrowseFile.Size = new System.Drawing.Size(129, 26);
            this.buttonBrowseFile.TabIndex = 2;
            this.buttonBrowseFile.Text = "Browse Wsdl ...";
            this.buttonBrowseFile.Click += new System.EventHandler(this.buttonBrowseFile_Click);
            // 
            // tabPageInvoke
            // 
            this.tabPageInvoke.Controls.Add(this.splitterInvoke);
            this.tabPageInvoke.Controls.Add(this.panelRightInvoke);
            this.tabPageInvoke.Controls.Add(this.panelLeftInvoke);
            this.tabPageInvoke.Location = new System.Drawing.Point(4, 22);
            this.tabPageInvoke.Name = "tabPageInvoke";
            this.tabPageInvoke.Size = new System.Drawing.Size(864, 516);
            this.tabPageInvoke.TabIndex = 0;
            this.tabPageInvoke.Tag = "";
            this.tabPageInvoke.Text = "Invoke";
            // 
            // splitterInvoke
            // 
            this.splitterInvoke.Location = new System.Drawing.Point(250, 0);
            this.splitterInvoke.Name = "splitterInvoke";
            this.splitterInvoke.Size = new System.Drawing.Size(3, 516);
            this.splitterInvoke.TabIndex = 0;
            this.splitterInvoke.TabStop = false;
            // 
            // panelRightInvoke
            // 
            this.panelRightInvoke.Controls.Add(this.labelOutputValue);
            this.panelRightInvoke.Controls.Add(this.labelInputValue);
            this.panelRightInvoke.Controls.Add(this.labelOutput);
            this.panelRightInvoke.Controls.Add(this.labelInput);
            this.panelRightInvoke.Controls.Add(this.treeInput);
            this.panelRightInvoke.Controls.Add(this.treeOutput);
            this.panelRightInvoke.Controls.Add(this.propOutput);
            this.panelRightInvoke.Controls.Add(this.propInput);
            this.panelRightInvoke.Controls.Add(this.buttonInvoke);
            this.panelRightInvoke.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRightInvoke.Location = new System.Drawing.Point(250, 0);
            this.panelRightInvoke.Name = "panelRightInvoke";
            this.panelRightInvoke.Size = new System.Drawing.Size(614, 516);
            this.panelRightInvoke.TabIndex = 1;
            this.panelRightInvoke.SizeChanged += new System.EventHandler(this.PanelRightInvoke_SizeChanged);
            // 
            // labelOutputValue
            // 
            this.labelOutputValue.Location = new System.Drawing.Point(326, 321);
            this.labelOutputValue.Name = "labelOutputValue";
            this.labelOutputValue.Size = new System.Drawing.Size(68, 17);
            this.labelOutputValue.TabIndex = 0;
            this.labelOutputValue.Text = "Value";
            // 
            // labelInputValue
            // 
            this.labelInputValue.Location = new System.Drawing.Point(326, 9);
            this.labelInputValue.Name = "labelInputValue";
            this.labelInputValue.Size = new System.Drawing.Size(68, 17);
            this.labelInputValue.TabIndex = 1;
            this.labelInputValue.Text = "Value";
            // 
            // labelOutput
            // 
            this.labelOutput.Location = new System.Drawing.Point(10, 321);
            this.labelOutput.Name = "labelOutput";
            this.labelOutput.Size = new System.Drawing.Size(76, 17);
            this.labelOutput.TabIndex = 2;
            this.labelOutput.Text = "Output";
            // 
            // labelInput
            // 
            this.labelInput.Location = new System.Drawing.Point(10, 9);
            this.labelInput.Name = "labelInput";
            this.labelInput.Size = new System.Drawing.Size(134, 17);
            this.labelInput.TabIndex = 3;
            this.labelInput.Text = "Input";
            // 
            // treeInput
            // 
            this.treeInput.HideSelection = false;
            this.treeInput.Location = new System.Drawing.Point(10, 26);
            this.treeInput.Name = "treeInput";
            this.treeInput.Size = new System.Drawing.Size(307, 272);
            this.treeInput.TabIndex = 4;
            this.treeInput.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeInput_AfterSelect);
            // 
            // treeOutput
            // 
            this.treeOutput.Location = new System.Drawing.Point(10, 346);
            this.treeOutput.Name = "treeOutput";
            this.treeOutput.Size = new System.Drawing.Size(307, 166);
            this.treeOutput.TabIndex = 5;
            this.treeOutput.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeOutput_AfterSelect);
            // 
            // propOutput
            // 
            this.propOutput.HelpVisible = false;
            this.propOutput.LineColor = System.Drawing.SystemColors.ScrollBar;
            this.propOutput.Location = new System.Drawing.Point(326, 346);
            this.propOutput.Name = "propOutput";
            this.propOutput.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.propOutput.Size = new System.Drawing.Size(279, 166);
            this.propOutput.TabIndex = 6;
            this.propOutput.ToolbarVisible = false;
            // 
            // propInput
            // 
            this.propInput.HelpVisible = false;
            this.propInput.LineColor = System.Drawing.SystemColors.ScrollBar;
            this.propInput.Location = new System.Drawing.Point(326, 26);
            this.propInput.Name = "propInput";
            this.propInput.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.propInput.Size = new System.Drawing.Size(279, 272);
            this.propInput.TabIndex = 7;
            this.propInput.ToolbarVisible = false;
            this.propInput.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propInput_PropertyValueChanged);
            // 
            // buttonInvoke
            // 
            this.buttonInvoke.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonInvoke.Location = new System.Drawing.Point(538, 312);
            this.buttonInvoke.Name = "buttonInvoke";
            this.buttonInvoke.Size = new System.Drawing.Size(67, 26);
            this.buttonInvoke.TabIndex = 8;
            this.buttonInvoke.Text = "Invoke";
            this.buttonInvoke.Click += new System.EventHandler(this.buttonInvoke_Click);
            // 
            // panelLeftInvoke
            // 
            this.panelLeftInvoke.Controls.Add(this.treeMethods);
            this.panelLeftInvoke.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeftInvoke.Location = new System.Drawing.Point(0, 0);
            this.panelLeftInvoke.Name = "panelLeftInvoke";
            this.panelLeftInvoke.Size = new System.Drawing.Size(250, 516);
            this.panelLeftInvoke.TabIndex = 2;
            // 
            // treeMethods
            // 
            this.treeMethods.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeMethods.HideSelection = false;
            this.treeMethods.Location = new System.Drawing.Point(0, 0);
            this.treeMethods.Name = "treeMethods";
            this.treeMethods.Size = new System.Drawing.Size(250, 516);
            this.treeMethods.TabIndex = 0;
            this.treeMethods.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeMethods_AfterSelect);
            // 
            // tabPageWsdl
            // 
            this.tabPageWsdl.Controls.Add(this.splitterWsdl);
            this.tabPageWsdl.Controls.Add(this.panelRightWsdl);
            this.tabPageWsdl.Controls.Add(this.panelLeftWsdl);
            this.tabPageWsdl.Location = new System.Drawing.Point(4, 22);
            this.tabPageWsdl.Name = "tabPageWsdl";
            this.tabPageWsdl.Size = new System.Drawing.Size(911, 634);
            this.tabPageWsdl.TabIndex = 2;
            this.tabPageWsdl.Tag = "";
            this.tabPageWsdl.Text = "WSDLs & Proxy";
            // 
            // splitterWsdl
            // 
            this.splitterWsdl.Location = new System.Drawing.Point(250, 0);
            this.splitterWsdl.Name = "splitterWsdl";
            this.splitterWsdl.Size = new System.Drawing.Size(3, 634);
            this.splitterWsdl.TabIndex = 0;
            this.splitterWsdl.TabStop = false;
            // 
            // panelRightWsdl
            // 
            this.panelRightWsdl.Controls.Add(this.richWsdl);
            this.panelRightWsdl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRightWsdl.Location = new System.Drawing.Point(250, 0);
            this.panelRightWsdl.Name = "panelRightWsdl";
            this.panelRightWsdl.Size = new System.Drawing.Size(661, 634);
            this.panelRightWsdl.TabIndex = 1;
            // 
            // richWsdl
            // 
            this.richWsdl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richWsdl.HideSelection = false;
            this.richWsdl.Location = new System.Drawing.Point(0, 0);
            this.richWsdl.Name = "richWsdl";
            this.richWsdl.ReadOnly = true;
            this.richWsdl.Size = new System.Drawing.Size(661, 634);
            this.richWsdl.TabIndex = 0;
            this.richWsdl.Text = "";
            this.richWsdl.WordWrap = false;
            // 
            // panelLeftWsdl
            // 
            this.panelLeftWsdl.Controls.Add(this.treeWsdl);
            this.panelLeftWsdl.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeftWsdl.Location = new System.Drawing.Point(0, 0);
            this.panelLeftWsdl.Name = "panelLeftWsdl";
            this.panelLeftWsdl.Size = new System.Drawing.Size(250, 634);
            this.panelLeftWsdl.TabIndex = 2;
            // 
            // treeWsdl
            // 
            this.treeWsdl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeWsdl.Location = new System.Drawing.Point(0, 0);
            this.treeWsdl.Name = "treeWsdl";
            this.treeWsdl.Size = new System.Drawing.Size(250, 634);
            this.treeWsdl.TabIndex = 0;
            this.treeWsdl.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeWsdl_AfterSelect);
            // 
            // tabPageMessage
            // 
            this.tabPageMessage.Controls.Add(this.richMessage);
            this.tabPageMessage.Location = new System.Drawing.Point(4, 22);
            this.tabPageMessage.Name = "tabPageMessage";
            this.tabPageMessage.Size = new System.Drawing.Size(911, 634);
            this.tabPageMessage.TabIndex = 3;
            this.tabPageMessage.Tag = "";
            this.tabPageMessage.Text = "Messages";
            // 
            // richMessage
            // 
            this.richMessage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richMessage.Location = new System.Drawing.Point(0, 0);
            this.richMessage.Name = "richMessage";
            this.richMessage.ReadOnly = true;
            this.richMessage.Size = new System.Drawing.Size(911, 634);
            this.richMessage.TabIndex = 0;
            this.richMessage.Text = "";
            // 
            // tabPageRaw
            // 
            this.tabPageRaw.Controls.Add(this.splitterRaw);
            this.tabPageRaw.Controls.Add(this.panelRightRaw);
            this.tabPageRaw.Controls.Add(this.panelLeftRaw);
            this.tabPageRaw.Location = new System.Drawing.Point(4, 22);
            this.tabPageRaw.Name = "tabPageRaw";
            this.tabPageRaw.Size = new System.Drawing.Size(911, 634);
            this.tabPageRaw.TabIndex = 1;
            this.tabPageRaw.Text = "Request/Response";
            // 
            // splitterRaw
            // 
            this.splitterRaw.Location = new System.Drawing.Point(250, 0);
            this.splitterRaw.Name = "splitterRaw";
            this.splitterRaw.Size = new System.Drawing.Size(3, 634);
            this.splitterRaw.TabIndex = 0;
            this.splitterRaw.TabStop = false;
            // 
            // panelRightRaw
            // 
            this.panelRightRaw.Controls.Add(this.buttonSend);
            this.panelRightRaw.Controls.Add(this.richRequest);
            this.panelRightRaw.Controls.Add(this.richResponse);
            this.panelRightRaw.Controls.Add(this.labelRequest);
            this.panelRightRaw.Controls.Add(this.labelResponse);
            this.panelRightRaw.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelRightRaw.Location = new System.Drawing.Point(250, 0);
            this.panelRightRaw.Name = "panelRightRaw";
            this.panelRightRaw.Size = new System.Drawing.Size(661, 634);
            this.panelRightRaw.TabIndex = 1;
            this.panelRightRaw.SizeChanged += new System.EventHandler(this.PanelRightRaw_SizeChanged);
            // 
            // buttonSend
            // 
            this.buttonSend.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.buttonSend.Location = new System.Drawing.Point(835, 336);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new System.Drawing.Size(67, 26);
            this.buttonSend.TabIndex = 0;
            this.buttonSend.Text = "Send";
            this.buttonSend.Click += new System.EventHandler(this.buttonSend_Click);
            // 
            // richRequest
            // 
            this.richRequest.Location = new System.Drawing.Point(288, 26);
            this.richRequest.Name = "richRequest";
            this.richRequest.Size = new System.Drawing.Size(614, 293);
            this.richRequest.TabIndex = 1;
            this.richRequest.Text = "";
            this.richRequest.WordWrap = false;
            // 
            // richResponse
            // 
            this.richResponse.Location = new System.Drawing.Point(288, 370);
            this.richResponse.Name = "richResponse";
            this.richResponse.ReadOnly = true;
            this.richResponse.Size = new System.Drawing.Size(614, 293);
            this.richResponse.TabIndex = 2;
            this.richResponse.Text = "";
            this.richResponse.WordWrap = false;
            // 
            // labelRequest
            // 
            this.labelRequest.Location = new System.Drawing.Point(288, 9);
            this.labelRequest.Name = "labelRequest";
            this.labelRequest.Size = new System.Drawing.Size(173, 17);
            this.labelRequest.TabIndex = 3;
            this.labelRequest.Text = "Request";
            // 
            // labelResponse
            // 
            this.labelResponse.Location = new System.Drawing.Point(288, 353);
            this.labelResponse.Name = "labelResponse";
            this.labelResponse.Size = new System.Drawing.Size(134, 17);
            this.labelResponse.TabIndex = 4;
            this.labelResponse.Text = "Response";
            // 
            // panelLeftRaw
            // 
            this.panelLeftRaw.Controls.Add(this.propRequest);
            this.panelLeftRaw.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelLeftRaw.Location = new System.Drawing.Point(0, 0);
            this.panelLeftRaw.Name = "panelLeftRaw";
            this.panelLeftRaw.Size = new System.Drawing.Size(250, 634);
            this.panelLeftRaw.TabIndex = 2;
            this.panelLeftRaw.SizeChanged += new System.EventHandler(this.PanelLeftRaw_SizeChanged);
            // 
            // propRequest
            // 
            this.propRequest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propRequest.HelpVisible = false;
            this.propRequest.LineColor = System.Drawing.SystemColors.ScrollBar;
            this.propRequest.Location = new System.Drawing.Point(0, 0);
            this.propRequest.Name = "propRequest";
            this.propRequest.PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
            this.propRequest.Size = new System.Drawing.Size(250, 634);
            this.propRequest.TabIndex = 0;
            this.propRequest.ToolbarVisible = false;
            // 
            // tabMain
            // 
            this.tabMain.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tabMain.Controls.Add(this.tabPageInvoke);
            this.tabMain.Controls.Add(this.tabPageRaw);
            this.tabMain.Controls.Add(this.tabPageWsdl);
            this.tabMain.Controls.Add(this.tabPageMessage);
            this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabMain.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabMain.ItemSize = new System.Drawing.Size(42, 18);
            this.tabMain.Location = new System.Drawing.Point(0, 0);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(872, 542);
            this.tabMain.TabIndex = 0;
            this.tabMain.SelectedIndexChanged += new System.EventHandler(this.tabMain_SelectedIndexChanged);
            // 
            // panelTopMain
            // 
            this.panelTopMain.Controls.Add(this.label1);
            this.panelTopMain.Controls.Add(this.txtCertName);
            this.panelTopMain.Controls.Add(this.labelEndPointUrl);
            this.panelTopMain.Controls.Add(this.textEndPointUri);
            this.panelTopMain.Controls.Add(this.buttonBrowseFile);
            this.panelTopMain.Controls.Add(this.buttonGet);
            this.panelTopMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTopMain.Location = new System.Drawing.Point(0, 0);
            this.panelTopMain.Name = "panelTopMain";
            this.panelTopMain.Size = new System.Drawing.Size(872, 97);
            this.panelTopMain.TabIndex = 0;
            this.panelTopMain.Paint += new System.Windows.Forms.PaintEventHandler(this.panelTopMain_Paint);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 51);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 12);
            this.label1.TabIndex = 5;
            this.label1.Text = "x509 Cert";
            // 
            // txtCertName
            // 
            this.txtCertName.Location = new System.Drawing.Point(106, 47);
            this.txtCertName.Name = "txtCertName";
            this.txtCertName.Size = new System.Drawing.Size(532, 21);
            this.txtCertName.TabIndex = 4;
            // 
            // panelBottomMain
            // 
            this.panelBottomMain.Controls.Add(this.tabMain);
            this.panelBottomMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBottomMain.Location = new System.Drawing.Point(0, 97);
            this.panelBottomMain.Name = "panelBottomMain";
            this.panelBottomMain.Size = new System.Drawing.Size(872, 542);
            this.panelBottomMain.TabIndex = 1;
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 14);
            this.ClientSize = new System.Drawing.Size(872, 639);
            this.Controls.Add(this.panelBottomMain);
            this.Controls.Add(this.panelTopMain);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Menu = this.mainMenu1;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = ".NET WebService Studio";
            this.tabPageInvoke.ResumeLayout(false);
            this.panelRightInvoke.ResumeLayout(false);
            this.panelLeftInvoke.ResumeLayout(false);
            this.tabPageWsdl.ResumeLayout(false);
            this.panelRightWsdl.ResumeLayout(false);
            this.panelLeftWsdl.ResumeLayout(false);
            this.tabPageMessage.ResumeLayout(false);
            this.tabPageRaw.ResumeLayout(false);
            this.panelRightRaw.ResumeLayout(false);
            this.panelLeftRaw.ResumeLayout(false);
            this.tabMain.ResumeLayout(false);
            this.panelTopMain.ResumeLayout(false);
            this.panelTopMain.PerformLayout();
            this.panelBottomMain.ResumeLayout(false);
            this.ResumeLayout(false);

    }

    private void InvokeWebMethod()
    {
      MethodProperty currentMethodProperty = this.GetCurrentMethodProperty();
      if (currentMethodProperty != null)
      {
        HttpWebClientProtocol proxy = currentMethodProperty.GetProxyProperty().GetProxy();

        RequestProperties properties = new RequestProperties(proxy);
        try
        {
          MethodInfo method = currentMethodProperty.GetMethod();
          System.Type declaringType = method.DeclaringType;
          WSSWebRequest.RequestTrace = properties;

          if (!string.IsNullOrEmpty(txtCertName.Text))
          {
            X509Certificate _clientCert = wsdl.GetClientCertificate(txtCertName.Text, StoreLocation.LocalMachine);
            WSSWebRequest.ClientCertificates = new X509CertificateCollection();
            WSSWebRequest.ClientCertificates.Add(_clientCert);
          }

          object[] parameters = currentMethodProperty.ReadChildren() as object[];
          object result = method.Invoke(proxy, BindingFlags.Public, null, parameters, null);
          this.treeOutput.Nodes.Clear();
          MethodProperty property2 = new MethodProperty(currentMethodProperty.GetProxyProperty(), method, result,
            parameters);
          property2.RecreateSubtree(null);
          this.treeOutput.Nodes.Add(property2.TreeNode);
          this.treeOutput.ExpandAll();
        }
        catch (Exception ex)
        {
          TabPage selectedTab = this.tabMain.SelectedTab;
          this.tabMain.SelectedTab = this.tabPageMessage;
          ShowMessage(this, MessageType.Failure, ex.Message);
          ShowMessage(this, MessageType.Failure, ex.StackTrace);
          ShowMessage(this, MessageType.Failure, ex.InnerException.Message);

        }
        finally
        {
          WSSWebRequest.RequestTrace = null;
          this.propRequest.SelectedObject = properties;
          this.richRequest.Text = properties.requestPayLoad;
          this.richResponse.Text = properties.responsePayLoad;
        }
      }
    }

    private bool IsValidCopyNode(TreeNodeProperty tnp)
    {
      return (((tnp != null) && (tnp.TreeNode.Parent != null)) && (tnp.GetType() != typeof(TreeNodeProperty)));
    }

    private bool IsValidPasteNode(TreeNodeProperty tnp)
    {
      IDataObject dataObject = Clipboard.GetDataObject();
      if ((dataObject == null) || (dataObject.GetData(DataFormats.Text) == null))
      {
        return false;
      }
      return this.IsValidCopyNode(tnp);
    }

    [STAThread]
    private static void Main()
    {
      Version version = typeof(string).Assembly.GetName().Version;
      isV1 = ((version.Major == 1) && (version.Minor == 0)) && (version.Build == 0xce4);
      mainForm = new MainForm();
      WSSWebRequestCreate.RegisterPrefixes();
      try
      {
        mainForm.SetupAssemblyResolver();
      }
      catch (Exception exception)
      {
        MessageBox.Show(null, exception.ToString(), "Error Setting up Assembly Resolver");
      }
      Application.Run(mainForm);
    }

    private void MainForm_SizeChanged(object sender, EventArgs e)
    {
      this.tabMain.Width = (base.Location.X + base.Width) - this.tabMain.Location.X;
      this.tabMain.Height = (base.Location.Y + base.Height) - this.tabMain.Location.Y;
    }

    private void menuItemAbout_Click(object sender, EventArgs e)
    {
      MessageBox.Show(this, ".NET Web Service Studio\n Version: 2.3.0");
    }

    private void menuItemExit_Click(object sender, EventArgs e)
    {
      base.Close();
    }

    private void menuItemFind_Click(object sender, EventArgs e)
    {
      SearchDialog dialog = new SearchDialog();
      dialog.ShowDialog();
      if (dialog.DialogResult == DialogResult.OK)
      {
        this.tabMain.SelectedTab = this.tabPageWsdl;
        this.findOption = RichTextBoxFinds.None;
        if (dialog.MatchCase)
        {
          this.findOption |= RichTextBoxFinds.MatchCase;
        }
        if (dialog.WholeWord)
        {
          this.findOption |= RichTextBoxFinds.WholeWord;
        }
        this.searchStr = dialog.SearchStr;
        this.Find();
      }
    }

    private void menuItemFindNext_Click(object sender, EventArgs e)
    {
      if (this.tabMain.SelectedTab == this.tabPageInvoke)
      {
        MessageBox.Show(this, "'Find' cannot be used in the 'Invoke' tab");
      }
      else
      {
        this.Find();
      }
    }

    private void menuItemHelp_Click(object sender, EventArgs e)
    {
      MessageBox.Show(this, MiniHelpText);
    }

    private void menuItemOpen_Click(object sender, EventArgs e)
    {
      this.openWsdlDialog.ShowDialog();
      string fileName = this.openWsdlDialog.FileName;
      Cursor cursor = this.Cursor;
      this.Cursor = Cursors.WaitCursor;
      try
      {
        this.wsdl.Reset();
        this.wsdl.Paths.Add(fileName);
        this.wsdl.Generate();
        this.FillWsdlTab();
        this.FillInvokeTab();
      }
      finally
      {
        this.Cursor = cursor;
      }
    }

    private void menuItemOptions_Click(object sender, EventArgs e)
    {
      new OptionDialog().ShowDialog();
    }

    private void menuItemSaveAll_Click(object sender, EventArgs e)
    {
      if ((this.saveAllDialog.ShowDialog() == DialogResult.OK) && ((((this.wsdl.Wsdls != null) && (this.wsdl.Wsdls.Count != 0)) || ((this.wsdl.Xsds != null) && (this.wsdl.Xsds.Count != 0))) || (this.wsdl.ProxyCode != null)))
      {
        int length = this.saveAllDialog.FileName.LastIndexOf('.');
        string str = (length >= 0) ? this.saveAllDialog.FileName.Substring(0, length) : this.saveAllDialog.FileName;
        if (this.wsdl.Wsdls.Count == 1)
        {
          this.SaveFile(str + ".wsdl", this.wsdl.Wsdls[0]);
        }
        else
        {
          for (int i = 0; i < this.wsdl.Wsdls.Count; i++)
          {
            this.SaveFile(str + i.ToString() + ".wsdl", this.wsdl.Wsdls[i]);
          }
        }
        if (this.wsdl.Xsds.Count == 1)
        {
          this.SaveFile(str + ".xsd", this.wsdl.Xsds[0]);
        }
        else
        {
          for (int j = 0; j < this.wsdl.Xsds.Count; j++)
          {
            this.SaveFile(str + j.ToString() + ".xsd", this.wsdl.Xsds[j]);
          }
        }
        this.SaveFile(str + "." + this.wsdl.ProxyFileExtension, this.wsdl.ProxyCode);
        this.SaveFile(str + "Client." + this.wsdl.ProxyFileExtension, Script.GetUsingCode(this.wsdl.WsdlProperties.Language) + "\n" + this.GenerateClientCode() + "\n" + Script.GetDumpCode(this.wsdl.WsdlProperties.Language));
      }
    }

    public Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
      Assembly proxyAssembly = this.wsdl.ProxyAssembly;
      if ((proxyAssembly != null) && (proxyAssembly.GetName().ToString() == args.Name))
      {
        return proxyAssembly;
      }
      return null;
    }

    private void PanelLeftRaw_SizeChanged(object sender, EventArgs e)
    {
      this.propRequest.SetBounds(0, 0, this.panelLeftRaw.Width, this.panelLeftRaw.Height, BoundsSpecified.Size);
    }

    private void PanelRightInvoke_SizeChanged(object sender, EventArgs e)
    {
      int width = (this.panelRightInvoke.Width - 0x18) / 2;
      int x = 8;
      int num3 = (8 + width) + 8;
      int height = (((this.panelRightInvoke.Height - 0x10) - 20) - 40) / 2;
      int y = 8;
      int num6 = (0x1c + height) + 20;
      this.labelInput.SetBounds(x, y, 0, 0, BoundsSpecified.Location);
      this.labelInputValue.SetBounds(num3, y, 0, 0, BoundsSpecified.Location);
      this.labelOutput.SetBounds(x, num6, 0, 0, BoundsSpecified.Location);
      this.labelOutputValue.SetBounds(num3, num6, 0, 0, BoundsSpecified.Location);
      y += 20;
      num6 += 20;
      this.treeInput.SetBounds(x, y, width, height, BoundsSpecified.All);
      this.treeOutput.SetBounds(x, num6, width, height, BoundsSpecified.All);
      this.propInput.SetBounds(num3, y, width, height, BoundsSpecified.All);
      this.propOutput.SetBounds(num3, num6, width, height, BoundsSpecified.All);
      this.buttonInvoke.SetBounds((num3 + width) - this.buttonInvoke.Width, ((this.panelRightInvoke.Height + 20) - this.buttonInvoke.Height) / 2, 0, 0, BoundsSpecified.Location);
    }

    private void PanelRightRaw_SizeChanged(object sender, EventArgs e)
    {
      int width = this.panelRightRaw.Width - 0x10;
      int x = 8;
      int height = (((this.panelRightRaw.Height - 0x10) - 20) - 40) / 2;
      int y = 8;
      int num5 = (0x1c + height) + 20;
      this.labelRequest.SetBounds(x, y, 0, 0, BoundsSpecified.Location);
      this.labelResponse.SetBounds(x, num5, 0, 0, BoundsSpecified.Location);
      y += 20;
      num5 += 20;
      this.richRequest.SetBounds(x, y, width, height, BoundsSpecified.All);
      this.richResponse.SetBounds(x, num5, width, height, BoundsSpecified.All);
      this.buttonSend.SetBounds((x + width) - this.buttonSend.Width, ((this.panelRightRaw.Height + 20) - this.buttonSend.Height) / 2, 0, 0, BoundsSpecified.Location);
    }

    private void propInput_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
    {
      TreeNodeProperty selectedObject = this.propInput.SelectedObject as TreeNodeProperty;
      if ((selectedObject != null) && ((e.ChangedItem.Label == "Type") && (e.OldValue != selectedObject.Type)))
      {
        TreeNodeProperty property2 = TreeNodeProperty.CreateTreeNodeProperty(selectedObject);
        property2.TreeNode = selectedObject.TreeNode;
        property2.RecreateSubtree(null);
        this.treeInput.SelectedNode = property2.TreeNode;
      }
    }

    private bool SaveFile(string fileName, string contents)
    {
      if (System.IO.File.Exists(fileName) && (MessageBox.Show(this, "File " + fileName + " already exists. Overwrite?", "Warning", MessageBoxButtons.YesNo) != DialogResult.Yes))
      {
        return false;
      }
      FileStream stream = System.IO.File.OpenWrite(fileName);
      StreamWriter writer = new StreamWriter(stream);
      writer.Write(contents);
      writer.Flush();
      stream.SetLength(stream.Position);
      stream.Close();
      return true;
    }

    private void SendWebRequest()
    {
      Encoding encoding = new UTF8Encoding(true);
      RequestProperties selectedObject = this.propRequest.SelectedObject as RequestProperties;
      HttpWebRequest request = (HttpWebRequest)WebRequest.CreateDefault(new Uri(selectedObject.Url));
      if ((selectedObject.HttpProxy != null) && (selectedObject.HttpProxy.Length != 0))
      {
        request.Proxy = new WebProxy(selectedObject.HttpProxy);
      }
      request.Method = selectedObject.Method.ToString();
      request.ContentType = selectedObject.ContentType;
      request.Headers["SOAPAction"] = selectedObject.SOAPAction;
      request.SendChunked = selectedObject.SendChunked;
      request.AllowAutoRedirect = selectedObject.AllowAutoRedirect;
      request.AllowWriteStreamBuffering = selectedObject.AllowWriteStreamBuffering;
      request.KeepAlive = selectedObject.KeepAlive;
      request.Pipelined = selectedObject.Pipelined;
      request.PreAuthenticate = selectedObject.PreAuthenticate;
      request.Timeout = selectedObject.Timeout;
      HttpWebClientProtocol proxy = this.GetCurrentMethodProperty().GetProxyProperty().GetProxy();
      if (selectedObject.UseCookieContainer)
      {
        if (proxy.CookieContainer != null)
        {
          request.CookieContainer = proxy.CookieContainer;
        }
        else
        {
          request.CookieContainer = new CookieContainer();
        }
      }
      CredentialCache cache = new CredentialCache();
      bool flag = false;
      if ((selectedObject.BasicAuthUserName != null) && (selectedObject.BasicAuthUserName.Length != 0))
      {
        cache.Add(new Uri(selectedObject.Url), "Basic", new NetworkCredential(selectedObject.BasicAuthUserName, selectedObject.BasicAuthPassword));
        flag = true;
      }
      if (selectedObject.UseDefaultCredential)
      {
        cache.Add(new Uri(selectedObject.Url), "NTLM", (NetworkCredential)CredentialCache.DefaultCredentials);
        flag = true;
      }
      if (flag)
      {
        request.Credentials = cache;
      }
      if (selectedObject.Method == RequestProperties.HttpMethod.POST)
      {
        request.ContentLength = this.richRequest.Text.Length + encoding.GetPreamble().Length;
        StreamWriter writer = new StreamWriter(request.GetRequestStream(), encoding);
        writer.Write(this.richRequest.Text);
        writer.Close();
      }
      try
      {
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        this.DumpResponse(response);
        response.Close();
      }
      catch (WebException exception)
      {
        if (exception.Response != null)
        {
          this.DumpResponse((HttpWebResponse)exception.Response);
        }
        else
        {
          this.richResponse.Text = exception.ToString();
        }
      }
      catch (Exception exception2)
      {
        this.richResponse.Text = exception2.ToString();
      }
    }

    private void SetupAssemblyResolver()
    {
      ResolveEventHandler handler = new ResolveEventHandler(this.OnAssemblyResolve);
      AppDomain.CurrentDomain.AssemblyResolve += handler;
    }

    public static void ShowMessage(object sender, MessageType status, string message)
    {
      if (mainForm != null)
      {
        mainForm.ShowMessageInternal(sender, status, message);
      }
    }

    private void ShowMessageInternal(object sender, MessageType status, string message)
    {
      if (message == null)
      {
        message = status.ToString();
      }
      switch (status)
      {
        case MessageType.Begin:
          this.richMessage.SelectionColor = Color.Blue;
          this.richMessage.AppendText(message + "\n");
          this.richMessage.Update();
          break;

        case MessageType.Success:
          this.richMessage.SelectionColor = Color.Green;
          this.richMessage.AppendText(message + "\n");
          this.richMessage.Update();
          if (sender == this.wsdl)
          {
            base.BeginInvoke(new WsdlGenerationDoneCallback(this.WsdlGenerationDone), new object[] { true });
          }
          break;

        case MessageType.Failure:
          this.richMessage.SelectionColor = Color.Red;
          this.richMessage.AppendText(message + "\n");
          this.richMessage.Update();
          if (sender == this.wsdl)
          {
            base.BeginInvoke(new WsdlGenerationDoneCallback(this.WsdlGenerationDone), new object[] { false });
          }
          break;

        case MessageType.Warning:
          this.richMessage.SelectionColor = Color.DarkRed;
          this.richMessage.AppendText(message + "\n");
          this.richMessage.Update();
          break;

        case MessageType.Error:
          this.richMessage.SelectionColor = Color.Red;
          this.richMessage.AppendText(message + "\n");
          this.richMessage.Update();
          break;
      }
    }

    private void tabMain_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (this.tabMain.SelectedTab == this.tabPageRaw)
      {
        if (this.propRequest.SelectedObject == null)
        {
          this.propRequest.SelectedObject = new RequestProperties(null);
        }
      }
      else if (((this.tabMain.SelectedTab == this.tabPageWsdl) && (this.treeWsdl.Nodes != null)) && (this.treeWsdl.Nodes.Count != 0))
      {
        TreeNode node = this.treeWsdl.Nodes[3];
        node.Tag = this.GenerateClientCode();
        if (this.treeWsdl.SelectedNode == node)
        {
          this.richWsdl.Text = node.Tag.ToString();
        }
      }
    }

    private void textEndPointUri_KeyPress(object sender, KeyPressEventArgs e)
    {
      if ((e.KeyChar == '\r') || (e.KeyChar == '\n'))
      {
        this.buttonGet_Click(sender, null);
        e.Handled = true;
      }
      else if (!char.IsControl(e.KeyChar))
      {
        if (!isV1)
        {
          this.textEndPointUri.SelectedText = e.KeyChar.ToString();
        }
        e.Handled = true;
        string text = this.textEndPointUri.Text;
        if ((text != null) && (text.Length != 0))
        {
          for (int i = 0; i < this.textEndPointUri.Items.Count; i++)
          {
            if (((string)this.textEndPointUri.Items[i]).StartsWith(text))
            {
              this.textEndPointUri.SelectedIndex = i;
              this.textEndPointUri.Select(text.Length, this.textEndPointUri.Text.Length);
              break;
            }
          }
        }
      }
    }

    private void treeInput_AfterSelect(object sender, TreeViewEventArgs e)
    {
      this.propInput.SelectedObject = e.Node.Tag;
      this.menuItemTreeInputCopy.Enabled = this.IsValidCopyNode(e.Node.Tag as TreeNodeProperty);
      this.menuItemTreeInputPaste.Enabled = this.IsValidPasteNode(e.Node.Tag as TreeNodeProperty);
    }

    private void treeInputMenuCopy_Click(object sender, EventArgs e)
    {
      this.CopyToClipboard(this.treeInput.SelectedNode.Tag as TreeNodeProperty);
    }

    private void treeInputMenuPaste_Click(object sender, EventArgs e)
    {
      TreeNodeProperty tag = this.treeInput.SelectedNode.Tag as TreeNodeProperty;
      if (tag is MethodProperty)
      {
        throw new Exception("Paste not valid on method");
      }
      System.Type[] typeList = tag.GetTypeList();
      System.Type type = typeof(DataSet).IsAssignableFrom(typeList[0]) ? typeof(DataSet) : typeof(object);
      XmlSerializer serializer = new XmlSerializer(type, typeList);
      StringReader textReader = new StringReader((string)Clipboard.GetDataObject().GetData(DataFormats.Text));
      object val = serializer.Deserialize(textReader);
      if ((val == null) || !typeList[0].IsAssignableFrom(val.GetType()))
      {
        throw new Exception("Invalid Type pasted");
      }
      TreeNodeProperty property2 = TreeNodeProperty.CreateTreeNodeProperty(tag, val);
      property2.TreeNode = tag.TreeNode;
      property2.RecreateSubtree(null);
      this.treeInput.SelectedNode = property2.TreeNode;
    }

    private void treeMethods_AfterSelect(object sender, TreeViewEventArgs e)
    {
      if (e.Node.Tag is MethodInfo)
      {
        MethodInfo tag = e.Node.Tag as MethodInfo;
        this.treeInput.Nodes.Clear();
        MethodProperty property = new MethodProperty(this.GetProxyPropertyFromNode(e.Node), tag);
        property.RecreateSubtree(null);
        this.treeInput.Nodes.Add(property.TreeNode);
        e.Node.Tag = property.TreeNode;
      }
      else if (e.Node.Tag is TreeNode)
      {
        this.treeInput.Nodes.Clear();
        this.treeInput.Nodes.Add((TreeNode)e.Node.Tag);
      }
      this.treeInput.ExpandAll();
      this.treeInput.SelectedNode = this.treeInput.Nodes[0];
    }

    private void treeOutput_AfterSelect(object sender, TreeViewEventArgs e)
    {
      this.propOutput.SelectedObject = e.Node.Tag;
      this.menuItemTreeOutputCopy.Enabled = this.IsValidCopyNode(e.Node.Tag as TreeNodeProperty);
    }

    private void treeOutputMenuCopy_Click(object sender, EventArgs e)
    {
      this.CopyToClipboard(this.treeOutput.SelectedNode.Tag as TreeNodeProperty);
    }

    private void treeWsdl_AfterSelect(object sender, TreeViewEventArgs e)
    {
      if ((e.Node.Tag != null) && (this.richWsdl.Tag != e.Node.Tag))
      {
        this.richWsdl.Text = e.Node.Tag.ToString();
        this.richWsdl.Tag = e.Node.Tag;
      }
      XmlTreeNode node = e.Node as XmlTreeNode;
      if (node != null)
      {
        this.richWsdl.Select(node.StartPosition, node.EndPosition - node.StartPosition);
      }
    }

    private void WsdlGenerationDone(bool genDone)
    {
      this.buttonGet.Text = "Get";
      this.FillWsdlTab();
      if (genDone)
      {
        this.ShowMessageInternal(this, MessageType.Begin, "Reflecting Proxy Assembly");
        this.FillInvokeTab();
        this.tabMain.SelectedTab = this.tabPageInvoke;
        this.ShowMessageInternal(this, MessageType.Success, "Ready To Invoke");
        Configuration.MasterConfig.InvokeSettings.CertName = this.txtCertName.Text;
        Configuration.MasterConfig.InvokeSettings.AddUri(this.textEndPointUri.Text);
        this.textEndPointUri.Items.Clear();
        this.textEndPointUri.Items.AddRange(Configuration.MasterConfig.InvokeSettings.RecentlyUsedUris);
      }
    }

    private delegate void WsdlGenerationDoneCallback(bool genDone);

    private void panelTopMain_Paint(object sender, PaintEventArgs e)
    {

    }
  }
}


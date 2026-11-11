namespace WinFormsMudBlazor
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            BlazorWebView = new Microsoft.AspNetCore.Components.WebView.WindowsForms.BlazorWebView();
            SuspendLayout();
            // 
            // BlazorWebView
            // 
            BlazorWebView.Dock = DockStyle.Fill;
            BlazorWebView.Location = new Point(0, 0);
            BlazorWebView.Margin = new Padding(3, 4, 3, 4);
            BlazorWebView.Name = "BlazorWebView";
            BlazorWebView.Size = new Size(1125, 801);
            BlazorWebView.TabIndex = 0;
            BlazorWebView.Text = "BlazorWebView";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1125, 801);
            Controls.Add(BlazorWebView);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 4, 3, 4);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WinFormsMudBlazor - Automação & Receitas";
            ResumeLayout(false);
        }

        #endregion

        public Microsoft.AspNetCore.Components.WebView.WindowsForms.BlazorWebView BlazorWebView;
    }
}

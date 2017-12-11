using System.Drawing;
namespace Svt.Caspar.Controls
{
	partial class FlashTemplateHostControl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.shockwaveFlashControl1 = new Svt.Caspar.Controls.ShockwaveFlashControl();
            this.SuspendLayout();
            // 
            // shockwaveFlashControl1
            // 
            this.shockwaveFlashControl1.Location = new System.Drawing.Point(0, 0);
            this.shockwaveFlashControl1.Name = "shockwaveFlashControl1";
            this.shockwaveFlashControl1.Size = new System.Drawing.Size(315, 254);
            this.shockwaveFlashControl1.TabIndex = 0;
            // 
            // FlashTemplateHostControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.shockwaveFlashControl1);
            this.Name = "FlashTemplateHostControl";
            this.Size = new System.Drawing.Size(315, 254);
            this.SizeChanged += new System.EventHandler(this.control_Resize);
            this.ResumeLayout(false);

		}

		#endregion

		private ShockwaveFlashControl shockwaveFlashControl1;

	}
}

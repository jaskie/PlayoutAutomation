namespace Svt.Caspar.Controls
{
	partial class ShockwaveFlashControl
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShockwaveFlashControl));
			this.axShockwaveFlash1 = new AxShockwaveFlashObjects.AxShockwaveFlash();
			((System.ComponentModel.ISupportInitialize)(this.axShockwaveFlash1)).BeginInit();
			this.SuspendLayout();
			// 
			// axShockwaveFlash1
			// 
			this.axShockwaveFlash1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.axShockwaveFlash1.Enabled = true;
			this.axShockwaveFlash1.Location = new System.Drawing.Point(0, 0);
			this.axShockwaveFlash1.Name = "axShockwaveFlash1";
			this.axShockwaveFlash1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axShockwaveFlash1.OcxState")));
			this.axShockwaveFlash1.Size = new System.Drawing.Size(311, 269);
			this.axShockwaveFlash1.TabIndex = 0;
			// 
			// ShockwaveFlashControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.axShockwaveFlash1);
			this.Name = "ShockwaveFlashControl";
			this.Size = new System.Drawing.Size(311, 269);
			((System.ComponentModel.ISupportInitialize)(this.axShockwaveFlash1)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private AxShockwaveFlashObjects.AxShockwaveFlash axShockwaveFlash1;
	}
}

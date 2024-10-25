using System.Drawing;
using System.Windows.Forms;

namespace AscomTesting.Forms
{
    public partial class ImageViewer : Form
    {
        public Point Offset { get; private set; }
        public float ZoomFactor { get; private set; }

        public ImageViewer(Image image)
        {
            InitializeComponent();

            // Set initial values.
            ImageContainer.Image = image;
            ImageContainer.Size = image.Size;
            Size sizeDelta = new Size(Width - ClientRectangle.Width,
                                      Height - ClientRectangle.Height);
            Size = new Size(image.Size.Width + sizeDelta.Width,
                            image.Size.Height + sizeDelta.Height);
        }

        private void SetImageTransformations()
        {
            ImageContainer.Location = new Point(-Offset.X, -Offset.Y);
            ImageContainer.Size = new Size((int)(ImageContainer.Image.Size.Width * ZoomFactor),
                                           (int)(ImageContainer.Image.Size.Height * ZoomFactor));
            Invalidate(true);
        }

        private bool mouseDrag = false;
        private Point prevMousePos;
        protected override void OnMouseDown(MouseEventArgs e)
        {
            mouseDrag = true;
            prevMousePos = Cursor.Position;
            SetImageTransformations();
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (mouseDrag)
            {
                Point curPoint = Cursor.Position;
                Point delta = new Point(curPoint.X - prevMousePos.X, curPoint.Y - prevMousePos.Y);
                Offset = new Point(Offset.X + delta.X, Offset.Y + delta.Y);
                prevMousePos = curPoint;
                SetImageTransformations();
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (mouseDrag)
            {
                Point curPoint = Cursor.Position;
                Point delta = new Point(curPoint.X - prevMousePos.X, curPoint.Y - prevMousePos.Y);
                Offset = new Point(Offset.X + delta.X, Offset.Y + delta.Y);
                mouseDrag = false;
                SetImageTransformations();
            }
            base.OnMouseUp(e);
        }
    }
}

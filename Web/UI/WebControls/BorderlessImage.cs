namespace Ecng.Web.UI.WebControls
{
	using System.Web.UI.WebControls;

	// https://forums.asp.net/t/1208356.aspx?Remove+border+width+0px+from+asp+Image
	public class BorderlessImage : Image
	{
		public override Unit BorderWidth
		{
			get
			{
				if (base.BorderWidth.IsEmpty)
					return Unit.Pixel(0);
				else
					return base.BorderWidth;
			}
			set => base.BorderWidth = value;
		}
	}
}
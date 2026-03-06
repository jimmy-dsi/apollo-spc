namespace Jimbl.Graphics;

using JMath;

public partial class Color {
	JVector3I vec3 => (JVector3I) vec;

	public Space DefaultSpace {
		set {
			ArithSpace  = value;
			BlendSpace  = value;
			FilterSpace = value;
		}
	}

	public Space ArithSpace  { get; set; } = Space.RGB;
	public Space BlendSpace  { get; set; } = Space.LRGB;
	public Space FilterSpace { get; set; } = Space.LRGB;
	
	public byte Red {
		get => (byte) Math.Clamp(vec.X, 0, 255);
		set => vec.X = value;
	}
	
	public byte Green {
		get => (byte) Math.Clamp(vec.Y, 0, 255);
		set => vec.Y = value;
	}
	
	public byte Blue {
		get => (byte) Math.Clamp(vec.Z, 0, 255);
		set => vec.Z = value;
	}
	
	public byte Alpha {
		get => (byte) Math.Clamp(vec.W, 0, 255);
		set => vec.W = value;
	}
	
	// Other vector representations
	public Vec3B RGB  {
		get => getRGB();
		set => getRGB().Inner = value;
	}
	public Vec3D SRGB {
		get => getSRGB();
		set => getSRGB().Inner = value;
	}
	public Vec3D LRGB {
		get => getLRGB();
		set => getLRGB().Inner = value;
	}
	public Vec3D HSV  {
		get => getHSV();
		set => getHSV() .Inner = value;
	}
	public Vec3D HSL  {
		get => getHSL();
		set => getHSL() .Inner = value;
	}
	public Vec3D XYZ  {
		get => getXYZ();
		set => getXYZ() .Inner = value;
	}
	public Vec3D Lab  {
		get => getLab();
		set => getLab() .Inner = value;
	}
	public Vec3D LCh  {
		get => getLCh();
		set => getLCh() .Inner = value;
	}
	
	public Vec4B RGBA  {
		get => getRGBA();
		set => getRGBA().Inner = value;
	}
	public Vec4D SRGBA {
		get => getSRGBA();
		set => getSRGBA().Inner = value;
	}
	public Vec4D LRGBA {
		get => getLRGBA();
		set => getLRGBA().Inner = value;
	}
	public Vec4D HSVA  {
		get => getHSVA();
		set => getHSVA() .Inner = value;
	}
	public Vec4D HSLA  {
		get => getHSLA();
		set => getHSLA() .Inner = value;
	}
	public Vec4D XYZA  {
		get => getXYZA();
		set => getXYZA() .Inner = value;
	}
	public Vec4D LabA  {
		get => getLabA();
		set => getLabA() .Inner = value;
	}
	public Vec4D LChA  {
		get => getLChA();
		set => getLChA() .Inner = value;
	}
}
namespace Jimbl.Graphics;

using System.Numerics;
using System.Diagnostics;
using DataStructs;
using JMath;

public partial class Color {
	public enum Space {
		RGB, LRGB, HSV, HSL, XYZ, Lab, LCh
	}
	
	public const double Gamma = 2.2;
	//static Cache<(JVector3I, Space), JVector3D> rgbToOtherCache = new(sizeLimit: 512);
	//static Cache<(JVector3D, Space), JVector3I> otherToRgbCache = new(sizeLimit: 512);
	
	JVector4I vec  = new();
	
	// Other vector representations
	Vec3B? rgb  = null;
	Vec3D? srgb = null;
	Vec3D? lrgb = null;
	Vec3D? hsv  = null;
	Vec3D? hsl  = null;
	Vec3D? xyz  = null;
	Vec3D? lab  = null;
	Vec3D? lch  = null;
	
	Vec4B? rgba  = null;
	Vec4D? srgba = null;
	Vec4D? lrgba = null;
	Vec4D? hsva  = null;
	Vec4D? hsla  = null;
	Vec4D? xyza  = null;
	Vec4D? laba  = null;
	Vec4D? lcha  = null;
	
	bool inited = false;
}
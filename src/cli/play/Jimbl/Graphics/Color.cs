using System.Diagnostics;

namespace Jimbl.Graphics;

using JMath;

public class Color {
	public enum Space {
		RGB, RGB_g, HSV, HSL, Lab, LCH
	}
	
	public static double Gamma = 2.2;
	
	JVector3B vec = new();
	
	public byte R {
		get => vec.X;
		set => vec.X = value;
	}
	
	public byte G {
		get => vec.Y;
		set => vec.Y = value;
	}
	
	public byte B {
		get => vec.Z;
		set => vec.Z = value;
	}
	
	public byte A {
		get /*=> vec.W*/;
		set /*=> vec.W = value*/;
	}
	
	public Color(byte r, byte g, byte b, byte a = 255) {
		vec[0] = r;
		vec[1] = g;
		vec[2] = b;
	}
	
	public Color(double x, double y, double z, double a = 1.0, Space colorSpace = Space.RGB) {
		switch (colorSpace) {
			case Space.RGB: {
				setRGB(x, y, z, a);
				break;
			}
			
			case Space.RGB_g: {
				break;
			}
			
			case Space.HSV: {
				break;
			}
			
			case Space.HSL: {
				break;
			}
			
			case Space.Lab: {
				break;
			}
			
			case Space.LCH: {
				break;
			}
			
			default: {
				throw new UnreachableException();
			}
		}
	}
	
	public byte this[int channel] {
		get => vec[channel];
		set => vec[channel] = value;
	}
	
	void setRGB(double r, double g, double b, double a = 1.0) {
		vec[0] = (byte) Math.Clamp(r * 256, 0, 255);
		vec[1] = (byte) Math.Clamp(g * 256, 0, 255);
		vec[2] = (byte) Math.Clamp(b * 256, 0, 255);
	}
}
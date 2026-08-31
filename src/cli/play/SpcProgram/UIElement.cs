namespace SpcProgram;

public class UIElement {
	Type type;
	KeyBindings.Action action;
	
	bool hasBeenClicked = false;
	bool active = true;
	
	public bool HighlightOnHover { get; private set; }
	
	public int X { get; private set; }
	public int Y { get; private set; }
	
	public int Width  { get; private set; }
	public int Height { get; private set; }
	
	public bool Active {
		get => active;
		set {
			active = value;
			if (!active) {
				hasBeenClicked = false;
			}
		}
	}
	
	public static UIElement[] ActiveElements = CliMain.BRRViewerUIElements;
	
	public UIElement(Type type, KeyBindings.Action action, int x, int y, int width, int height, bool highlightOnHover = true) {
		this.type   = type;
		this.action = action;
		
		HighlightOnHover = highlightOnHover;
		
		X = x;
		Y = y;
		
		Width  = width;
		Height = height;
	}
	
	public KeyBindings.Action? TriggeredAction() {
		if (!active) return null;
		
		switch (type) {
			// Requires a full press and release
			case Type.ClickableButton_1: {
				if (IsDepressed() && InputListener.MouseButtonReleased(MouseEventType.LeftClick)) {
					hasBeenClicked = false;
					return action;
				}
				else if (OverlapsCursor() && InputListener.MouseButtonPressed(MouseEventType.LeftClick)) {
					hasBeenClicked = true;
				}
				else if (OverlapsCursor() && !InputListener.MouseButtonDown(MouseEventType.LeftClick)) {
					hasBeenClicked = false;
				}
				
				break;
			}
			
			// Triggers instantly on press
			case Type.ClickableButton_2: {
				if (OverlapsCursor() && InputListener.MouseButtonPressed(MouseEventType.LeftClick)) {
					return action;
				}
				break;
			}
			
			case Type.ScrollableArea: {
				break;
			}
			
			case Type.SeekBar: {
				break;
			}
		}
		
		return null;
	}
	
	public bool OverlapsCursor() {
		var mx = InputListener.MouseX;
		var my = InputListener.MouseY;
		
		return mx >= X && mx < X + Width && my >= Y && my < Y + Height;
	}
	
	public bool Overlaps(int x, int y) {
		return x >= X && x < X + Width && y >= Y && y < Y + Height;
	}
	
	public bool IsDepressed() {
		return type == Type.ClickableButton_1
		       && hasBeenClicked && OverlapsCursor() && Overlaps(InputListener.LeftClickMouseX, InputListener.LeftClickMouseY);
	}
	
	public enum Type {
		ClickableButton_1,
		ClickableButton_2,
		ScrollableArea,
		SeekBar
	}
}
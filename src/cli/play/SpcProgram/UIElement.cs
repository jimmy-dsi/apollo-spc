namespace SpcProgram;

public class UIElement {
	Type type;
	KeyBindings.Action  action;
	KeyBindings.Action? action_2;
	
	bool hasBeenClicked = false;
	bool active = true;
	bool useCustomTrigger = false;
	
	public bool HighlightOnHover { get; private set; }
	
	public int X { get; private set; }
	public int Y { get; private set; }
	
	public int Width  { get; private set; }
	public int Height { get; private set; }
	
	public bool TriggerSignal { get; private set; } = false;
	
	public Type UIType => type;
	
	public bool Active {
		get => active;
		set {
			active = value;
			if (!active) {
				TriggerSignal  = false;
				hasBeenClicked = false;
			}
		}
	}
	
	public static UIElement[] ActiveElements = [];
	
	public UIElement(Type type,
	                 KeyBindings.Action action,
	                 KeyBindings.Action? action_2,
	                 int x, int y,
	                 int width, int height,
	                 bool highlightOnHover = true,
	                 bool useCustomTrigger = false) {
		this.type     = type;
		this.action   = action;
		this.action_2 = action_2;
		
		HighlightOnHover = highlightOnHover;
		this.useCustomTrigger = useCustomTrigger;
		
		X = x;
		Y = y;
		
		Width  = width;
		Height = height;
	}
	
	public KeyBindings.Action? TriggeredAction() {
		if (!active) return null;
		
		switch (type) {
			// Requires a full press and release
			case Type.ClickableButton_1 or Type.ClickableButton_3: {
				if (IsDepressed() && InputListener.MouseButtonReleased(MouseEventType.LeftClick)) {
					hasBeenClicked = false;
					if (useCustomTrigger) TriggerSignal = true;
					return action;
				}
				else if (OverlapsCursor() && InputListener.MouseButtonPressed(MouseEventType.LeftClick)) {
					hasBeenClicked = true;
					TriggerSignal  = false;
					return KeyBindings.Action.Null; // Do this to prevent firing of other buttons' actions
				}
				else if (OverlapsCursor() && !InputListener.MouseButtonDown(MouseEventType.LeftClick)) {
					hasBeenClicked = false;
					TriggerSignal  = false;
					return KeyBindings.Action.Null; // Do this to prevent firing of other buttons' actions
				}
				else {
					TriggerSignal  = false;
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
				if (OverlapsCursor()) {
					if (InputListener.MouseButtonPressed(MouseEventType.ScrollWheelUp)) {
						return action;
					}
					else if (InputListener.MouseButtonPressed(MouseEventType.ScrollWheelDown)) {
						return action_2;
					}
				}
				
				break;
			}
			
			case Type.ScrollableAreaH: {
				if (OverlapsCursor()) {
					if (InputListener.MouseButtonPressed(MouseEventType.ScrollWheelUp)) {
						return action;
					}
					else if (InputListener.MouseButtonPressed(MouseEventType.ScrollWheelDown)) {
						return action_2;
					}
				}
				
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
		return type is Type.ClickableButton_1 or Type.ClickableButton_3
		       && hasBeenClicked && OverlapsCursor() && Overlaps(InputListener.LeftClickMouseX, InputListener.LeftClickMouseY);
	}
	
	public enum Type {
		ClickableButton_1,
		ClickableButton_2,
		ClickableButton_3,
		ScrollableArea,
		ScrollableAreaH,
		SeekBar
	}
}
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

internal class DecalBlendModeDrawer : MaterialPropertyDrawer {
  public enum BlendMode {
    None,
    //
    Darken,
    Multiply,
    ColorBurn,
    LinearBurn,
    DarkerColor,
    //
    Lighten,
    Screen,
    ColorDodge,
    LinearDodge,
    LighterColor,
    //
    Overlay,
    SoftLight,
    HardLight,
    VividLight,
    LinearLight,
    PinLight,
    HardMix,
    //
    Difference,
    Exclusion,
    Subtract,
    Divide,
    //
    Hue,
    Color,
    Saturation,
    Luminosity
  }

  Dictionary<BlendMode, string> keywords = new Dictionary<BlendMode, string>() {
    {BlendMode.Darken, "FD_DARKEN"},
    {BlendMode.Multiply, "FD_MULTIPLY"},
    {BlendMode.ColorBurn, "FD_COLORBURN"},
    {BlendMode.LinearBurn, "FD_LINEARBURN"},
    {BlendMode.DarkerColor, "FD_DARKERCOLOR"},
    //
    {BlendMode.Lighten, "FD_LIGHTEN"},
    {BlendMode.Screen, "FD_SCREEN"},
    {BlendMode.ColorDodge, "FD_COLORDODGE"},
    {BlendMode.LinearDodge, "FD_LINEARDODGE"},
    {BlendMode.LighterColor, "FD_LIGHTERCOLOR"},
    //
    {BlendMode.Overlay, "FD_OVERLAY"},
    {BlendMode.SoftLight, "FD_SOFTLIGHT"},
    {BlendMode.HardLight, "FD_HARDLIGHT"},
    {BlendMode.VividLight, "FD_VIVIDLIGHT"},
    {BlendMode.LinearLight, "FD_LINEARLIGHT"},
    {BlendMode.PinLight, "FD_PINLIGHT"},
    {BlendMode.HardMix, "FD_HARDMIX"},
    //
    {BlendMode.Difference, "FD_DIFFERENCE"},
    {BlendMode.Exclusion, "FD_EXCLUSION"},
    {BlendMode.Subtract, "FD_SUBTRACT"},
    {BlendMode.Divide, "FD_DIVIDE"},
    //
    {BlendMode.Hue, "FD_HUE"},
    {BlendMode.Color, "FD_COLOR"},
    {BlendMode.Saturation, "FD_SATURATION"},
    {BlendMode.Luminosity, "FD_LUMINOSITY"},
  };

  public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor) {
    EditorGUI.showMixedValue = prop.hasMixedValue;
    var mode = (BlendMode)prop.floatValue;

    EditorGUI.BeginChangeCheck();
    mode = (BlendMode)EditorGUI.EnumPopup(position, label, mode);
    if(EditorGUI.EndChangeCheck()) {
      editor.RegisterPropertyChangeUndo(prop.displayName);
      prop.floatValue = (float)mode;

      var targets = prop.targets;
      for(int i=0; i < targets.Length; ++i) {
        var mat = (Material)targets[i];

        // Disable all keywords except the requested one.
        foreach(var kv in keywords) {
          if(mode == kv.Key) {
            mat.EnableKeyword(kv.Value);
          } else {
            mat.DisableKeyword(kv.Value);
          }
        }
      }
    }

    EditorGUI.showMixedValue = false;
  }
}
#endif

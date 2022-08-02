﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TableRange
{
    public class TableRangeGizmo : Command_Toggle
    {
        public const float PopupWidth   = 300f;
        public const float PopupHeight  =  40f;
        public const float Margin       =   8f;
        public const float LabelWidth   =  20f;

        public const int WinID = 15649835;

        private readonly TableState state;
        private Rect sliderRect;
        private bool sliderOpen = false;

        public TableRangeGizmo(Thing table)
        {
            state = State.Tables[table];
            isActive = state.IsActive;
            toggleAction = state.Toggle;
            defaultLabel = Strings.UseRange;
            defaultDesc = Strings.UseRangeTip;
            icon = Resources.UseRange;
        }

        protected override GizmoResult GizmoOnGUIInt(Rect butRect, GizmoRenderParms parms)
        {
            sliderOpen = state && (Mouse.IsOver(butRect) || (sliderOpen && sliderRect.ExpandedBy(2f).Contains(Event.current.mousePosition)));
            if (sliderOpen) Window(butRect);
            return base.GizmoOnGUIInt(butRect, parms);
        }

        private void Window(Rect gizmo)
        {
            if (gizmo.xMax + PopupWidth < Screen.width)
            {
                sliderRect = new Rect(gizmo.xMax, gizmo.y + (gizmo.height - PopupHeight) / 2, PopupWidth, PopupHeight);
            }
            else
            {
                sliderRect = new Rect(Mathf.Min(gizmo.x, Screen.width - PopupWidth), gizmo.y - PopupHeight, PopupWidth, PopupHeight);
            }
            Find.WindowStack.ImmediateWindow(WinID, sliderRect, WindowLayer.Dialog, DoSlider);
        }

        public void DoSlider()
        {
            float oldValue = state;
            float newValue = Slider(sliderRect.AtZero(), oldValue);
            if (oldValue != newValue) state.Range = newValue;
        }

        public static float Slider(Rect rect, float value)
        {
            Rect widget = new Rect(rect.x + Margin, rect.y, LabelWidth, rect.height);
            TextAnchor anchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(widget, Mathf.RoundToInt(value).ToString());
            Text.Anchor = anchor;
            widget.x += LabelWidth + Margin;
            widget.xMax = rect.xMax - Margin;

            return Widgets.HorizontalSlider(widget, value, 0f, MySettings.MaxRange, true, roundTo: 0.2f);
        }
    }
}


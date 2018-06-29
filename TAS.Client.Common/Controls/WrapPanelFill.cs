using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace TAS.Client.Common.Controls
{
	public class WrapPanelFill : WrapPanel
	{
		// ******************************************************************
		public static readonly DependencyProperty UseToFillProperty = DependencyProperty.RegisterAttached("UseToFill", typeof(bool), 
			typeof(WrapPanelFill), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

		// ******************************************************************
		public static void SetUseToFill(UIElement element, bool value)
		{
			element.SetValue(UseToFillProperty, value);
		}
		// ******************************************************************
		public static bool GetUseToFill(UIElement element)
		{
			return (bool)element.GetValue(UseToFillProperty);
		}

		// ******************************************************************

		// ******************************************************************

		// ******************************************************************
		private static bool DoubleGreaterThan(double value1, double value2)
		{
			return value1 > value2 && Math.Abs(value1-value2) > double.Epsilon;
		}

		// ******************************************************************
		private bool _atLeastOneElementCanHasItsWidthExpanded;

		// ******************************************************************
		/// <summary> 
		/// <see cref="FrameworkElement.MeasureOverride"/>
		/// </summary> 
		protected override Size MeasureOverride(Size constraint)
		{
			var curLineSize = new UvSize(Orientation);
			var panelSize = new UvSize(Orientation);
			var uvConstraint = new UvSize(Orientation, constraint.Width, constraint.Height);
			var itemWidth = ItemWidth;
			var itemHeight = ItemHeight;
			var itemWidthSet = !double.IsNaN(itemWidth);
			var itemHeightSet = !double.IsNaN(itemHeight);

			var childConstraint = new Size(
				itemWidthSet ? itemWidth : constraint.Width,
				itemHeightSet ? itemHeight : constraint.Height);

			var children = InternalChildren;

			// EO
			var currentLineInfo = new LineInfo(); // EO, the way it works it is always like we are on the current line
			_lineInfos.Clear();
			_atLeastOneElementCanHasItsWidthExpanded = false;

			for (int i = 0, count = children.Count; i < count; i++)
			{
				var child = children[i];
				if (child == null) continue;

				//Flow passes its own constrint to children 
				child.Measure(childConstraint);

				//this is the size of the child in UV space 
				var sz = new UvSize(
					Orientation,
					(itemWidthSet ? itemWidth : child.DesiredSize.Width),
					(itemHeightSet ? itemHeight : child.DesiredSize.Height));

				if (DoubleGreaterThan(curLineSize.U + sz.U, uvConstraint.U)) //need to switch to another line 
				{
					// EO
					currentLineInfo.Size = curLineSize;
					_lineInfos.Add(currentLineInfo);

					panelSize.U = Math.Max(curLineSize.U, panelSize.U);
					panelSize.V += curLineSize.V;
					curLineSize = sz;

					// EO
					currentLineInfo = new LineInfo();
					var feChild = child as FrameworkElement;
					if (GetUseToFill(feChild))
					{
						currentLineInfo.ElementsWithNoWidthSet.Add(feChild);
						_atLeastOneElementCanHasItsWidthExpanded = true;
					}

					if (DoubleGreaterThan(sz.U, uvConstraint.U)) //the element is wider then the constrint - give it a separate line
					{
						currentLineInfo = new LineInfo();

						panelSize.U = Math.Max(sz.U, panelSize.U);
						panelSize.V += sz.V;
						curLineSize = new UvSize(Orientation);
					}
				}
				else //continue to accumulate a line
				{
					curLineSize.U += sz.U;
					curLineSize.V = Math.Max(sz.V, curLineSize.V);

					// EO
					var feChild = child as FrameworkElement;
				    if (!GetUseToFill(feChild))
                        continue;
				    currentLineInfo.ElementsWithNoWidthSet.Add(feChild);
				    _atLeastOneElementCanHasItsWidthExpanded = true;
				}
			}

			if (curLineSize.U > 0)
			{
				currentLineInfo.Size = curLineSize;
				_lineInfos.Add(currentLineInfo);
			}

			//the last line size, if any should be added 
			panelSize.U = Math.Max(curLineSize.U, panelSize.U);
			panelSize.V += curLineSize.V;

			// EO
		    return _atLeastOneElementCanHasItsWidthExpanded
		        ? new Size(constraint.Width, panelSize.Height)
		        : new Size(panelSize.Width, panelSize.Height);

		    //go from UV space to W/H space
		}

		// ************************************************************************
		private struct UvSize
		{
			internal UvSize(Orientation orientation, double width, double height)
			{
				U = V = 0d;
				_orientation = orientation;
				Width = width;
				Height = height;
			}

			internal UvSize(Orientation orientation)
			{
				U = V = 0d;
				_orientation = orientation;
			}

			internal double U;
			internal double V;
			private readonly Orientation _orientation;

			internal double Width
			{
				get => _orientation == Orientation.Horizontal ? U : V;
			    private set { if (_orientation == Orientation.Horizontal) U = value; else V = value; }
			}
			internal double Height
			{
				get => _orientation == Orientation.Horizontal ? V : U;
			    private set { if (_orientation == Orientation.Horizontal) V = value; else U = value; }
			}
		}

		// ************************************************************************
		private class LineInfo
		{
			public readonly List<UIElement> ElementsWithNoWidthSet = new List<UIElement>();
			//			public double SpaceLeft = 0;
			//			public double WidthCorrectionPerElement = 0;
			public UvSize Size;
		}

		private readonly List<LineInfo> _lineInfos = new List<LineInfo>();

		// ************************************************************************
		/// <summary>
		/// <see cref="FrameworkElement.ArrangeOverride"/> 
		/// </summary> 
		protected override Size ArrangeOverride(Size finalSize)
		{
			var lineIndex = 0;
			var firstInLine = 0;
			var itemWidth = ItemWidth;
			var itemHeight = ItemHeight;
			var accumulatedV = 0d;
			var itemU = Orientation == Orientation.Horizontal ? itemWidth : itemHeight;
			var curLineSize = new UvSize(Orientation);
			var uvFinalSize = new UvSize(Orientation, finalSize.Width, finalSize.Height);
			var itemWidthSet = !double.IsNaN(itemWidth);
			var itemHeightSet = !double.IsNaN(itemHeight);
			var useItemU = Orientation == Orientation.Horizontal ? itemWidthSet : itemHeightSet;

			var children = InternalChildren;

			for (int i = 0, count = children.Count; i < count; i++)
			{
				var child = children[i];
				if (child == null) continue;

				var sz = new UvSize(
					Orientation,
					itemWidthSet ? itemWidth : child.DesiredSize.Width,
					itemHeightSet ? itemHeight : child.DesiredSize.Height);

				if (DoubleGreaterThan(curLineSize.U + sz.U, uvFinalSize.U)) //need to switch to another line 
				{
					ArrangeLine(lineIndex, accumulatedV, curLineSize.V, firstInLine, i, useItemU, itemU, uvFinalSize);
					lineIndex++;

					accumulatedV += curLineSize.V;
					curLineSize = sz;

					if (DoubleGreaterThan(sz.U, uvFinalSize.U)) //the element is wider then the constraint - give it a separate line 
					{
						//switch to next line which only contain one element 
						ArrangeLine(lineIndex, accumulatedV, sz.V, i, ++i, useItemU, itemU, uvFinalSize);

						accumulatedV += sz.V;
						curLineSize = new UvSize(Orientation);
					}
					firstInLine = i;
				}
				else //continue to accumulate a line
				{
					curLineSize.U += sz.U;
					curLineSize.V = Math.Max(sz.V, curLineSize.V);
				}
			}

			//arrange the last line, if any
			if (firstInLine < children.Count)
			{
				ArrangeLine(lineIndex, accumulatedV, curLineSize.V, firstInLine, children.Count, useItemU, itemU, uvFinalSize);
			}

			return finalSize;
		}

		// ************************************************************************
		private void ArrangeLine(int lineIndex, double v, double lineV, int start, int end, bool useItemU, double itemU, UvSize uvFinalSize)
		{
			double u = 0;
			var isHorizontal = (Orientation == Orientation.Horizontal);

			Debug.Assert(lineIndex < _lineInfos.Count);

			var lineInfo = _lineInfos[lineIndex];
			var lineSpaceAvailableForCorrection = Math.Max(uvFinalSize.U - lineInfo.Size.U, 0);
			double perControlCorrection = 0;
			if (lineSpaceAvailableForCorrection > 0 && lineInfo.Size.U > 0)
			{
				perControlCorrection = lineSpaceAvailableForCorrection / lineInfo.ElementsWithNoWidthSet.Count;
				if (double.IsInfinity(perControlCorrection))
				{
					perControlCorrection = 0;
				}
			}
			var indexOfControlToAdjustSizeToFill = 0;
			var uIElementToAdjustNext = indexOfControlToAdjustSizeToFill < lineInfo.ElementsWithNoWidthSet.Count ? lineInfo.ElementsWithNoWidthSet[indexOfControlToAdjustSizeToFill] : null;

			var children = InternalChildren;
			for (var i = start; i < end; i++)
			{
			    if (!(children[i] is UIElement child))
                    continue;
			    var childSize = new UvSize(Orientation, child.DesiredSize.Width, child.DesiredSize.Height);
			    var layoutSlotU = useItemU ? itemU : childSize.U;

			    if (perControlCorrection > 0 && ReferenceEquals(child, uIElementToAdjustNext))
			    {
			        layoutSlotU += perControlCorrection;

			        indexOfControlToAdjustSizeToFill++;
			        uIElementToAdjustNext = indexOfControlToAdjustSizeToFill < lineInfo.ElementsWithNoWidthSet.Count ? lineInfo.ElementsWithNoWidthSet[indexOfControlToAdjustSizeToFill] : null;
			    }
					
			    child.Arrange(new Rect(
			        isHorizontal ? u : v,
			        isHorizontal ? v : u,
			        isHorizontal ? layoutSlotU : lineV,
			        isHorizontal ? lineV : layoutSlotU));
			    u += layoutSlotU;
			}
		}

		// ************************************************************************

	}
}

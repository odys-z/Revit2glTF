# About the Fork

For trying and learning. Some useful resource:

[Jeremy Tammik, Graphics Pipeline Custom Exporter, July 08, 2013](https://thebuildingcoder.typepad.com/blog/2013/07/graphics-pipeline-custom-exporter.html)

[Revit 2017 API (and new versions), Autodesk](https://www.revitapidocs.com/2017.1/d4648875-d41a-783b-d5f4-638df39ee413.htm)

# Revit2glTF - A Revit glTF Exporter
This is currently a work in progress but the end goal is to create an open source implementation of an extensible exporter from Autodesk Revit to the glTF model format.

## Current To-Do's
- [x] Handle basic material export
- [ ] Handle textured material export
- [ ] Handle normals export
- [ ] Add toggle for exporting each element as a seperate .bin vs a single .glb.
- [x] Add element properties to extras on glTF nodes.
- [ ] Add element properties to a sqlite file referenced by glTF nodes.

<?xml version="1.0" encoding="UTF-8"?>
<StyledLayerDescriptor version="1.0.0" xmlns="http://www.opengis.net/sld" xmlns:ogc="http://www.opengis.net/ogc" xmlns:xlink="http://www.w3.org/1999/xlink" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <NamedLayer><Name>location-analysis-heatmap</Name><UserStyle><Title>Konum Analizi Isı Haritası</Title><FeatureTypeStyle>
    <Transformation><ogc:Function name="vec:Heatmap">
      <ogc:Function name="parameter"><ogc:Literal>data</ogc:Literal></ogc:Function>
      <ogc:Function name="parameter"><ogc:Literal>weightAttr</ogc:Literal><ogc:Literal>HeatWeight</ogc:Literal></ogc:Function>
      <ogc:Function name="parameter"><ogc:Literal>radiusPixels</ogc:Literal><ogc:Literal>32</ogc:Literal></ogc:Function>
      <ogc:Function name="parameter"><ogc:Literal>pixelsPerCell</ogc:Literal><ogc:Literal>2</ogc:Literal></ogc:Function>
      <ogc:Function name="parameter"><ogc:Literal>outputBBOX</ogc:Literal><ogc:Function name="env"><ogc:Literal>wms_bbox</ogc:Literal></ogc:Function></ogc:Function>
      <ogc:Function name="parameter"><ogc:Literal>outputWidth</ogc:Literal><ogc:Function name="env"><ogc:Literal>wms_width</ogc:Literal></ogc:Function></ogc:Function>
      <ogc:Function name="parameter"><ogc:Literal>outputHeight</ogc:Literal><ogc:Function name="env"><ogc:Literal>wms_height</ogc:Literal></ogc:Function></ogc:Function>
    </ogc:Function></Transformation>
    <Rule><RasterSymbolizer><Opacity>0.78</Opacity><ColorMap type="ramp">
      <ColorMapEntry color="#0000ff" quantity="0" opacity="0"/><ColorMapEntry color="#00ffff" quantity="0.25"/><ColorMapEntry color="#00ff00" quantity="0.5"/><ColorMapEntry color="#ffff00" quantity="0.75"/><ColorMapEntry color="#ff0000" quantity="1"/>
    </ColorMap></RasterSymbolizer></Rule>
  </FeatureTypeStyle></UserStyle></NamedLayer>
</StyledLayerDescriptor>

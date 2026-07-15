# GeoServer styles

## point_heatmap

`point_heatmap.sld` is used by the frontend heatmap WMS request:

```text
LAYERS=map_project:TblPoints
STYLES=point_heatmap
CQL_FILTER=IsDeleted=false AND UserId={loginUserId}
```

Manual setup in GeoServer:

1. Open GeoServer admin at `http://localhost:8080/geoserver`.
2. Go to `Styles` and choose `Add a new style`.
3. Set the style name to `point_heatmap` and workspace to `map_project`.
4. Upload or paste `geoserver/point_heatmap.sld`.
5. Save and validate the style.
6. Either assign it to `map_project:TblPoints` as an additional style, or leave it available by name. The frontend requests it with `STYLES=point_heatmap`.

The style uses GeoServer's `vec:Heatmap` rendering transformation. If the transformation is unavailable, enable/install the GeoServer rendering transformations support that matches your GeoServer version.

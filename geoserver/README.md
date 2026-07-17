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

## POI layer and category style

1. GeoServer'da `Stores > postgis` veri kaynağını açın ve `Publish` ekranından `TblPois` tablosunu yayınlayın.
2. Layer adı `map_project:TblPois`, declared SRS `EPSG:4326` olmalıdır. `Compute from data` ve ardından `Compute from native bounds` ile bounding box değerlerini hesaplayın.
3. `Styles > Add a new style` ekranında workspace olarak `map_project`, ad olarak `poi_category_style` seçin ve `poi_category_style.sld` dosyasını yükleyin.
4. `geoserver/poi-icons` klasörünü GeoServer data directory içindeki `workspaces/map_project/styles/poi-icons` klasörüne kopyalayın.
5. Stil `IconKey` alanındaki `pharmacy`, `cafe`, `restaurant`, `hospital`, `school`, `market` değerlerine göre SVG seçer. GeoServer alan adlarını küçük harfle yayınlıyorsa SLD'deki `IconKey` ve `Name` ifadelerini `iconkey` ve `name` yapın.
6. `TblPois > Publishing` ekranında bu stili default veya additional style olarak ekleyin. Frontend ayrıca WMS isteğinde `STYLES=poi_category_style` gönderir.
7. Layer Preview ile WMS görünümünü, WFS GeoJSON çıktısını test edin.

Aktif POI filtresi:

```text
CQL_FILTER=IsDeleted=false AND IsActive=true
```

GeoServer alanları küçük harf gösteriyorsa eşdeğeri `isdeleted=false AND isactive=true` biçimindedir. Etiketler yalnızca `MaxScaleDenominator=70000` altında, OpenLayers'ta yaklaşık zoom 14 ve daha yakın seviyelerde gösterilir.

## Konum Analizi katmanı

1. PostgreSQL store içinde `LocationAnalysisResults` tablosunu point feature type olarak publish edin. Katman adını `location_analysis_result` yapın ve native/declared SRID değerini EPSG:4326 seçin.
2. `location-analysis-heatmap.sld` dosyasını `location-analysis-heatmap` adıyla yeni style olarak yükleyin. GeoServer'ın `HeatWeight` kolonunu gördüğünü doğrulayın ve style'ı katmana bağlayın.
3. Layer Preview ile WMS görüntüsünü kontrol edin. Örnek filtre: `analysis_id='00000000-0000-0000-0000-000000000000' AND user_id=1 AND is_active=true`.
4. PostgreSQL isimleri küçük harfe dönüştürülmüşse frontend ortamında layer adını buna göre ayarlayın veya GeoServer feature type alias'ını yukarıdaki adla verin.

Frontend her analizde `analysis_id`, JWT'den alınmış mevcut `user_id` ve `is_active=true` koşullarını CQL_FILTER olarak yollar. Normal POI katmanı bu kullanıcı filtresini kullanmaz.

`ProvinceBoundaries` tablosu il seçim kaynağıdır. İl geometrileri frontend içine gömülmez; kurumunuzun güncel ve lisanslı Türkiye il sınırı verisini EPSG:4326 olarak bu tabloya yükleyin.

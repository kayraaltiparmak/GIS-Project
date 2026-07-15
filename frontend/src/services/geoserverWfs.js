import GeoJSON from "ol/format/GeoJSON";
import {
  GEOSERVER_CQL_FIELDS,
  GEOSERVER_DISABLE_CQL_FILTER,
  GEOSERVER_DEBUG,
  GEOSERVER_LAYERS,
  GEOSERVER_WFS_URL
} from "../config/geoserver";

const geoJsonFormat = new GeoJSON();

export function buildUserCqlFilter(userId) {
  if (GEOSERVER_DISABLE_CQL_FILTER || !userId) {
    return "";
  }

  return GEOSERVER_CQL_FIELDS.isDeleted + " = false AND " + GEOSERVER_CQL_FIELDS.userId + " = " + Number(userId);
}

export async function getFeaturesFromLayer({ type, cqlFilter, bbox, maxFeatures = 20 }) {
  const layer = GEOSERVER_LAYERS[type];

  if (!layer) {
    return [];
  }

  const params = new URLSearchParams({
    service: "WFS",
    version: "1.1.0",
    request: "GetFeature",
    typeName: layer.name,
    outputFormat: "application/json",
    srsName: "EPSG:3857",
    maxFeatures: String(maxFeatures)
  });

  if (cqlFilter) {
    params.set("CQL_FILTER", cqlFilter);
  }

  if (bbox) {
    params.set("bbox", bbox.join(",") + ",EPSG:3857");
  }

  const url = GEOSERVER_WFS_URL + "?" + params.toString();

  if (GEOSERVER_DEBUG) {
    console.log("[GeoServer WFS]", { layer: layer.name, cqlFilter, url });
  }

  const response = await fetch(url);

  if (!response.ok) {
    return [];
  }

  const data = await response.json();
  return geoJsonFormat.readFeatures(data, {
    dataProjection: "EPSG:3857",
    featureProjection: "EPSG:3857"
  });
}

export async function getFeaturesFromLayers({ types, cqlFilter, bbox, maxFeatures = 20 }) {
  const groups = await Promise.all(
    types.map((type) => getFeaturesFromLayer({ type, cqlFilter, bbox, maxFeatures }))
  );

  return groups.flatMap((features, index) =>
    features.map((feature) => ({
      feature,
      type: types[index]
    }))
  );
}

export const GEOSERVER_URL =
  import.meta.env.VITE_GEOSERVER_URL || "http://localhost:8080/geoserver";

export const GEOSERVER_WORKSPACE =
  import.meta.env.VITE_GEOSERVER_WORKSPACE || "map_project";

export const GEOSERVER_WMS_URL =
  import.meta.env.VITE_GEOSERVER_WMS_URL || GEOSERVER_URL + "/" + GEOSERVER_WORKSPACE + "/wms";

export const GEOSERVER_WFS_URL =
  import.meta.env.VITE_GEOSERVER_WFS_URL || GEOSERVER_URL + "/" + GEOSERVER_WORKSPACE + "/wfs";

export const GEOSERVER_CQL_FIELDS = {
  isDeleted: import.meta.env.VITE_GEOSERVER_IS_DELETED_FIELD || "isdeleted",
  userId: import.meta.env.VITE_GEOSERVER_USER_ID_FIELD || "userid"
};

export const GEOSERVER_DISABLE_CQL_FILTER =
  import.meta.env.VITE_GEOSERVER_DISABLE_CQL_FILTER === "true";

export const GEOSERVER_HEATMAP_STYLE =
  import.meta.env.VITE_GEOSERVER_HEATMAP_STYLE || "point_heatmap";

export const GEOSERVER_DEBUG =
  import.meta.env.DEV || import.meta.env.VITE_GEOSERVER_DEBUG === "true";

export const GEOSERVER_LAYERS = {
  Point: {
    name: GEOSERVER_WORKSPACE + ":TblPoints",
    endpoint: "TblPoint"
  },
  Line: {
    name: GEOSERVER_WORKSPACE + ":TblLines",
    endpoint: "TblLine"
  },
  Polygon: {
    name: GEOSERVER_WORKSPACE + ":TblPolygons",
    endpoint: "TblPolygon"
  }
};

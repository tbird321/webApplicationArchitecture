import { useEffect, useMemo, useState } from 'react';
import { useAppStateContext } from './useAppStateContext';

// Generic feature presets keyed by appType
// Extend this map per site as needed while keeping names generic
const FEATURE_PRESETS = {
  Alexis: {
    homePage: {
      renderGallery: true,
      galleryMinPages: 1,
      showGalleryHeader: false,
      showHeaderContentInGallery: false,
      collectionSelector: 'first', // future: 'byId', 'byName'
    },
  },
};

export function useHomePageFeatures({ config, pageModel }) {
  const { Authorization, WebSiteState, DatabaseProcessing } = useAppStateContext();
  const [galleryPages, setGalleryPages] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);

  const resolvedConfig = useMemo(() => config ?? Authorization.getConfiguration(), [Authorization, config]);

  const featureConfig = useMemo(() => {
    const appType = resolvedConfig?.Site?.appType;
    return FEATURE_PRESETS[appType] || { homePage: { renderGallery: false, galleryMinPages: 2 } };
  }, [resolvedConfig?.Site?.appType]);

  const isHomePage = useMemo(() => {
    const currentName = pageModel?.name;
    const homeName = resolvedConfig?.Site?.initialPage;
    return currentName && homeName && currentName === homeName;
  }, [pageModel?.name, resolvedConfig?.Site?.initialPage]);

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      if (!featureConfig.homePage.renderGallery || !isHomePage) {
        if (!cancelled) setGalleryPages([]);
        return;
      }
      setIsLoading(true);
      setError(null);
      try {
        const websiteId = resolvedConfig?.Site?.websiteId ?? WebSiteState.websiteID();
        const collections = await DatabaseProcessing.searchCollection({ websiteId });
        if (collections && collections.length > 0) {
          const target = collections[0];
          const detail = await DatabaseProcessing.getCollectionPage(target.id);
          if (!cancelled) setGalleryPages(detail?.pages || []);
        } else {
          if (!cancelled) setGalleryPages([]);
        }
      } catch (e) {
        if (!cancelled) {
          setError(e);
          setGalleryPages([]);
        }
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };

    load();
    return () => {
      cancelled = true;
    };
  }, [DatabaseProcessing, WebSiteState, featureConfig.homePage.renderGallery, isHomePage, resolvedConfig?.Site?.websiteId]);

  const shouldRenderGallery = useMemo(() => {
    if (!featureConfig.homePage.renderGallery) return false;
    if (!isHomePage) return false;
    return Array.isArray(galleryPages) && galleryPages.length >= (featureConfig.homePage.galleryMinPages || 2);
  }, [featureConfig.homePage.galleryMinPages, featureConfig.homePage.renderGallery, galleryPages, isHomePage]);

  const showGalleryHeader = useMemo(() => {
    // Config override wins; default to preset or false
    const cfg = resolvedConfig?.Site?.galleryHeaderEnabled;
    if (cfg === true) return true;
    if (cfg === false) return false;
    return !!featureConfig.homePage.showGalleryHeader;
  }, [resolvedConfig?.Site?.galleryHeaderEnabled, featureConfig.homePage.showGalleryHeader]);

  const showHeaderContentInGallery = useMemo(() => {
    const cfg = resolvedConfig?.Site?.galleryHeaderContentEnabled;
    if (cfg === true) return true;
    if (cfg === false) return false;
    return !!featureConfig.homePage.showHeaderContentInGallery;
  }, [resolvedConfig?.Site?.galleryHeaderContentEnabled, featureConfig.homePage.showHeaderContentInGallery]);

  return {
    shouldRenderGallery,
    galleryPages,
    isLoading,
    error,
    showGalleryHeader,
    showHeaderContentInGallery,
  };
}



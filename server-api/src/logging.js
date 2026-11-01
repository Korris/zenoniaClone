/** The path of a request URL, without its query string. */
export const pathOnly = (url) => {
  const i = String(url).indexOf('?');
  return i < 0 ? String(url) : String(url).slice(0, i);
};

/** Logger options of the real server. Query strings never reach the log, whatever a caller puts in them; bodies and headers are not logged at all. */
export function loggerOptions(level = 'info') {
  return {
    level,
    redact: ['req.headers.authorization', 'req.headers["x-service-key"]'],
    serializers: {
      req: (req) => ({ method: req.method, url: pathOnly(req.url), remoteAddress: req.ip }),
    },
  };
}

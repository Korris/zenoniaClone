# Runs the Unity Linux headless player. The player is not in the image: it is mounted read-only at /opt/zenonia (ZONE_BUILD_DIR).
# NOT RUN YET: no Linux build exists (the Unity Linux module is not installed). The library list is what a Unity Linux player usually
# needs; if the player reports a missing .so, add its package here.
FROM ubuntu:24.04
RUN apt-get update \
  && apt-get install -y --no-install-recommends ca-certificates libgcc-s1 libstdc++6 zlib1g libglib2.0-0t64 libgl1 libxcursor1 libxrandr2 libxi6 \
  && rm -rf /var/lib/apt/lists/* \
  && useradd --create-home --uid 1000 zone \
  && mkdir -p /data /opt/zenonia \
  && chown zone:zone /data
COPY entrypoint.sh /usr/local/bin/zone-entrypoint.sh
RUN chmod 755 /usr/local/bin/zone-entrypoint.sh && sed -i 's/\r$//' /usr/local/bin/zone-entrypoint.sh
USER zone
ENV HOME=/home/zone
ENTRYPOINT ["/usr/local/bin/zone-entrypoint.sh"]
